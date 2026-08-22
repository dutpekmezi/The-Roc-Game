var RocFirebaseWebGLLibrary = {
  $RocFirebaseWebGLBridge: {
  firebaseVersion: "12.14.0",
  appName: "the-roc-game-matcha-birdy",
  initialEnergy: 15,
  config: {
    apiKey: "AIzaSyAbLmDnDdebuazMxtUURMA5Za0OrlPKh30",
    authDomain: "matcha-birdy.firebaseapp.com",
    databaseURL: "https://matcha-birdy-default-rtdb.firebaseio.com",
    projectId: "matcha-birdy",
    storageBucket: "matcha-birdy.firebasestorage.app",
    messagingSenderId: "35631047429"
  },
  state: {
    initPromise: null,
    status: 0,
    userId: "",
    error: "",
    app: null,
    auth: null,
    db: null,
    functions: null,
    firebase: null,
    operations: {},
    nextOperationId: 1
  },

  makeString: function(value) {
    var text = value == null ? "" : String(value);
    var length = lengthBytesUTF8(text) + 1;
    var buffer = _malloc(length);
    stringToUTF8(text, buffer, length);
    return buffer;
  },

  loadScript: function(url) {
    return new Promise(function(resolve, reject) {
      var existing = document.querySelector("script[src='" + url + "']");
      if (existing) {
        if (existing.getAttribute("data-loaded") === "true") {
          resolve();
          return;
        }

        if (existing.getAttribute("data-load-error") === "true") {
          existing.remove();
        } else {
          existing.addEventListener("load", function() { resolve(); }, { once: true });
          existing.addEventListener("error", function() { reject(new Error("Failed to load " + url)); }, { once: true });
          return;
        }
      }

      var script = document.createElement("script");
      script.src = url;
      script.async = true;
      script.onload = function() {
        script.setAttribute("data-loaded", "true");
        resolve();
      };
      script.onerror = function() {
        script.setAttribute("data-load-error", "true");
        script.remove();
        reject(new Error("Failed to load " + url));
      };
      document.head.appendChild(script);
    });
  },

  loadFirebase: async function() {
    if (globalThis.firebase && globalThis.firebase.auth && globalThis.firebase.firestore && globalThis.firebase.functions) {
      return globalThis.firebase;
    }

    var baseUrl = "https://www.gstatic.com/firebasejs/" + this.firebaseVersion + "/";
    await this.loadScript(baseUrl + "firebase-app-compat.js");
    await this.loadScript(baseUrl + "firebase-auth-compat.js");
    await this.loadScript(baseUrl + "firebase-firestore-compat.js");
    await this.loadScript(baseUrl + "firebase-functions-compat.js");

    if (!globalThis.firebase || !globalThis.firebase.auth || !globalThis.firebase.firestore || !globalThis.firebase.functions) {
      throw new Error("Firebase Web SDK could not be loaded.");
    }

    return globalThis.firebase;
  },

  getOrCreateApp: function(firebase) {
    if (firebase.apps && firebase.apps.length > 0) {
      for (var i = 0; i < firebase.apps.length; i++) {
        if (firebase.apps[i] && firebase.apps[i].name === this.appName) {
          return firebase.apps[i];
        }
      }
    }

    return firebase.initializeApp(this.config, this.appName);
  },

  configureAuthPersistence: async function(auth) {
    if (!auth || typeof auth.setPersistence !== "function") {
      return;
    }

    try {
      var persistence = this.state.firebase
        && this.state.firebase.auth
        && this.state.firebase.auth.Auth
        && this.state.firebase.auth.Auth.Persistence
        ? this.state.firebase.auth.Auth.Persistence.LOCAL
        : null;

      if (persistence) {
        await auth.setPersistence(persistence);
        console.log("[RocFirebase] Auth persistence: LOCAL.");
      }
    } catch (error) {
      console.warn("[RocFirebase] Auth persistence could not be configured:", error);
    }
  },

  waitForAuthUser: function(auth) {
    return new Promise(function(resolve, reject) {
      if (!auth || typeof auth.onAuthStateChanged !== "function") {
        resolve(auth && auth.currentUser ? auth.currentUser : null);
        return;
      }

      if (auth.currentUser && auth.currentUser.uid) {
        resolve(auth.currentUser);
        return;
      }

      var completed = false;
      var unsubscribe = null;

      unsubscribe = auth.onAuthStateChanged(function(user) {
        if (completed) {
          return;
        }

        completed = true;
        if (unsubscribe) {
          unsubscribe();
        }

        resolve(user || auth.currentUser || null);
      }, function(error) {
        if (completed) {
          return;
        }

        completed = true;
        if (unsubscribe) {
          unsubscribe();
        }

        reject(error);
      });
    });
  },

  createGoogleProvider: function() {
    if (!this.state.firebase || !this.state.firebase.auth) {
      throw new Error("Firebase Auth SDK is not loaded.");
    }

    var provider = new this.state.firebase.auth.GoogleAuthProvider();
    provider.setCustomParameters({
      prompt: "select_account"
    });
    return provider;
  },

  isGoogleUser: function(user) {
    if (!user || !user.uid || !Array.isArray(user.providerData)) {
      return false;
    }

    for (var i = 0; i < user.providerData.length; i++) {
      if (user.providerData[i] && user.providerData[i].providerId === "google.com") {
        return true;
      }
    }

    return false;
  },

  beginGoogleRedirect: async function(auth) {
    if (!auth || typeof auth.signInWithRedirect !== "function") {
      throw new Error("Firebase Auth redirect sign-in is not available.");
    }

    console.log("[RocFirebase] Google user is required; starting Google redirect.");
    await auth.signInWithRedirect(this.createGoogleProvider());
    return await new Promise(function() {});
  },

  initialize: function() {
    var bridge = this;
    var state = bridge.state;

    if (state.status === 1) {
      return Promise.resolve(state.userId);
    }

    if (state.initPromise && state.status !== 2) {
      return state.initPromise;
    }

    state.status = 0;
    state.error = "";

    state.initPromise = (async function() {
      try {
        var firebase = await bridge.loadFirebase();
        state.firebase = firebase;
        state.app = bridge.getOrCreateApp(firebase);
        state.auth = firebase.auth(state.app);
        state.db = firebase.firestore(state.app);
        state.functions = firebase.functions(state.app);
        bridge.configureFirestoreTransport(state.db);
        await bridge.configureAuthPersistence(state.auth);

        if (typeof state.auth.getRedirectResult === "function") {
          await state.auth.getRedirectResult();
        }

        console.log("[RocFirebase] Waiting for persisted Firebase auth state.");
        var user = await bridge.waitForAuthUser(state.auth);
        if (!bridge.isGoogleUser(user)) {
          if (user && user.uid) {
            console.log("[RocFirebase] Existing Firebase user is not Google; signing out before Google redirect.");
            await state.auth.signOut();
          }

          await bridge.beginGoogleRedirect(state.auth);
          return "";
        }

        if (!user || !user.uid) {
          throw new Error("Google Firebase auth did not return a user.");
        }

        state.userId = user.uid;
        await bridge.ensureUserDocument(user.uid);
        state.status = 1;
        console.log("[RocFirebase] WebGL Firebase ready. uid=" + user.uid);
        return user.uid;
      } catch (error) {
        state.status = 2;
        state.error = bridge.getErrorMessage(error);
        state.initPromise = null;
        console.warn("[RocFirebase] WebGL Firebase init failed:", error);
        throw error;
      }
    })();

    return state.initPromise;
  },

  ensureReady: async function() {
    await this.initialize();
    if (this.state.status !== 1 || !this.state.db || !this.state.auth || !this.state.userId) {
      throw new Error(this.state.error || "Firebase is not ready.");
    }
  },

  serverTimestamp: function() {
    return this.state.firebase.firestore.FieldValue.serverTimestamp();
  },

  configureFirestoreTransport: function(db) {
    if (!db || typeof db.settings !== "function") {
      return;
    }

    try {
      db.settings({
        experimentalAutoDetectLongPolling: true,
        useFetchStreams: false,
        merge: true
      });
      console.log("[RocFirebase] Firestore transport: auto-detect long polling enabled.");
      return;
    } catch (error) {
      console.warn("[RocFirebase] Firestore auto-detect long polling skipped:", error);
    }

    try {
      db.settings({
        experimentalForceLongPolling: true,
        useFetchStreams: false,
        merge: true
      });
      console.log("[RocFirebase] Firestore transport: force long polling enabled.");
    } catch (error) {
      console.warn("[RocFirebase] Firestore transport settings skipped:", error);
    }
  },

  ensureUserDocument: async function(userId) {
    var uid = userId || this.state.userId;
    if (!uid || !this.state.db) {
      throw new Error("Cannot ensure user document before Firebase is ready.");
    }

    var userRef = this.state.db.collection("users").doc(uid);
    var userSnapshot = await userRef.get({ source: "server" });

    var userPayload = {
      userId: uid,
      lastSeenAt: this.serverTimestamp()
    };

    if (!userSnapshot.exists) {
      userPayload.createdAt = this.serverTimestamp();
    }

    await userRef.set(userPayload, { merge: true });

    await this.ensureInitialEnergyDocument(userRef);
  },

  ensureInitialEnergyDocument: async function(userRef) {
    var energyRef = userRef.collection("currencies").doc("energy");
    var snapshot = await energyRef.get({ source: "server" });
    if (snapshot.exists) {
      return;
    }

    await energyRef.set({
      amount: this.initialEnergy,
      updatedAt: this.serverTimestamp()
    });
  },

  clearCurrentUserData: async function() {
    await this.ensureReady();
    var userId = this.state.userId;
    var userRef = this.state.db.collection("users").doc(userId);

    await this.deleteCollection(userRef.collection("currencies"));
    await this.deleteCollection(userRef.collection("QRCodes"));
    await this.deleteCollection(userRef.collection("purchasedProducts"));
    await this.deleteCollection(userRef.collection("entitlements"));
    await userRef.delete();

    await this.state.auth.signOut();
    this.state.status = 0;
    this.state.userId = "";
    await this.beginGoogleRedirect(this.state.auth);
    return { userId: "" };
  },

  deleteCollection: async function(collectionRef) {
    var snapshot = await collectionRef.get();
    var deletes = [];
    snapshot.forEach(function(doc) {
      deletes.push(doc.ref.delete());
    });
    await Promise.all(deletes);
  },

  syncCurrencyAmount: async function(currencyId, amount) {
    await this.ensureReady();
    await this.state.db
      .collection("users")
      .doc(this.state.userId)
      .collection("currencies")
      .doc(currencyId)
      .set({
        amount: Math.max(0, Math.floor(Number(amount) || 0)),
        updatedAt: this.serverTimestamp()
      }, { merge: true });

    return { ok: true };
  },

  modifyCurrencyAmount: async function(currencyId, delta) {
    await this.ensureReady();

    var bridge = this;
    var id = currencyId ? String(currencyId) : "";
    var change = Math.floor(Number(delta) || 0);
    if (!id) {
      return {
        isSuccess: false,
        isInsufficient: false,
        balance: 0,
        error: "Gecersiz currency"
      };
    }

    var ref = bridge.state.db
      .collection("users")
      .doc(bridge.state.userId)
      .collection("currencies")
      .doc(id);

    return await bridge.state.db.runTransaction(async function(transaction) {
      var snapshot = await transaction.get(ref);
      var data = snapshot.exists ? (snapshot.data() || {}) : {};
      var currentBalance = Math.max(0, Math.floor(Number(data.amount) || 0));

      if (change < 0 && currentBalance < Math.abs(change)) {
        return {
          isSuccess: false,
          isInsufficient: true,
          balance: currentBalance,
          error: "Yetersiz bakiye"
        };
      }

      var nextBalance = Math.max(0, currentBalance + change);
      transaction.set(ref, {
        amount: nextBalance,
        updatedAt: bridge.serverTimestamp()
      }, { merge: true });

      return {
        isSuccess: true,
        isInsufficient: false,
        balance: nextBalance,
        error: ""
      };
    });
  },

  getCurrencyAmounts: async function() {
    await this.ensureReady();
    var snapshot = await this.state.db
      .collection("users")
      .doc(this.state.userId)
      .collection("currencies")
      .get({ source: "server" });

    var currencies = [];
    snapshot.forEach(function(doc) {
      var data = doc.data() || {};
      currencies.push({
        id: doc.id,
        amount: Math.max(0, Math.floor(Number(data.amount) || 0))
      });
    });

    return { currencies: currencies };
  },

  getCurrencyAmount: async function(currencyId) {
    await this.ensureReady();

    if (!currencyId) {
      throw new Error("Currency id is empty.");
    }

    var snapshot = await this.state.db
      .collection("users")
      .doc(this.state.userId)
      .collection("currencies")
      .doc(currencyId)
      .get({ source: "server" });

    var data = snapshot.exists ? (snapshot.data() || {}) : {};
    var balance = Math.max(0, Math.floor(Number(data.amount) || 0));

    console.log("[RocFirebase] Currency document read:", {
      userId: this.state.userId,
      currencyId: currencyId,
      exists: snapshot.exists,
      amount: balance
    });

    return {
      isSuccess: true,
      isInsufficient: false,
      balance: balance,
      error: ""
    };
  },

  getEnergyAmount: async function(maxEnergy) {
    await this.ensureReady();

    var max = Math.max(1, Math.floor(Number(maxEnergy) || 1));
    var snapshot = await this.state.db
      .collection("users")
      .doc(this.state.userId)
      .collection("currencies")
      .doc("energy")
      .get({ source: "server" });

    var data = snapshot.exists ? (snapshot.data() || {}) : {};
    var currentBalance = Math.max(0, Math.floor(Number(data.amount) || 0));
    var balance = Math.min(currentBalance, max);

    console.log("[RocFirebase] Energy document read:", {
      userId: this.state.userId,
      exists: snapshot.exists,
      amount: currentBalance,
      clamped: balance
    });

    return {
      isSuccess: true,
      isInsufficient: false,
      wasRefilled: false,
      balance: balance,
      error: ""
    };
  },

  claimDailyEnergy: async function(maxEnergy, dailyEnergy, cooldownHours) {
    return await this.getEnergyAmount(maxEnergy);
  },

  trySpendEnergy: async function(amount) {
    await this.ensureReady();

    var bridge = this;
    var spendAmount = Math.max(0, Math.floor(Number(amount) || 0));
    var ref = bridge.state.db
      .collection("users")
      .doc(bridge.state.userId)
      .collection("currencies")
      .doc("energy");

    return await bridge.state.db.runTransaction(async function(transaction) {
      var snapshot = await transaction.get(ref);
      var data = snapshot.exists ? (snapshot.data() || {}) : {};
      var currentBalance = Math.max(0, Math.floor(Number(data.amount) || 0));

      if (spendAmount <= 0) {
        return {
          isSuccess: true,
          isInsufficient: false,
          wasRefilled: false,
          balance: currentBalance,
          error: ""
        };
      }

      if (currentBalance < spendAmount) {
        return {
          isSuccess: false,
          isInsufficient: true,
          wasRefilled: false,
          balance: currentBalance,
          error: ""
        };
      }

      var nextBalance = currentBalance - spendAmount;
      transaction.set(ref, {
        amount: nextBalance,
        updatedAt: bridge.serverTimestamp()
      }, { merge: true });

      return {
        isSuccess: true,
        isInsufficient: false,
        wasRefilled: false,
        balance: nextBalance,
        error: ""
      };
    });
  },

  tryUseFreeSpin: async function(cooldownHours) {
    await this.ensureReady();

    var bridge = this;
    var cooldownMs = Math.max(1, Number(cooldownHours) || 1) * 60 * 60 * 1000;
    var ref = bridge.state.db
      .collection("users")
      .doc(bridge.state.userId)
      .collection("entitlements")
      .doc("dailySpin");

    return await bridge.state.db.runTransaction(async function(transaction) {
      var snapshot = await transaction.get(ref);
      var data = snapshot.exists ? (snapshot.data() || {}) : {};
      var lastUsedMs = bridge.timestampToMillis(data.lastUsedAt);
      var isAvailable = !lastUsedMs || Date.now() - lastUsedMs >= cooldownMs;

      if (!isAvailable) {
        return {
          isSuccess: false,
          isOnCooldown: true,
          error: ""
        };
      }

      transaction.set(ref, {
        lastUsedAt: bridge.serverTimestamp()
      }, { merge: true });

      return {
        isSuccess: true,
        isOnCooldown: false,
        error: ""
      };
    });
  },

  tryPurchaseProduct: async function(product) {
    await this.ensureReady();

    var productId = product && product.productId ? String(product.productId) : "";

    if (!productId) {
      return { isSuccess: false, qrPayload: "", error: "Gecersiz urun" };
    }

    return await this.callFunction("purchaseProduct", {
      productId: productId,
      productName: product && product.productName ? String(product.productName) : "",
      productDescription: product && product.productDescription ? String(product.productDescription) : "",
      section: product && product.section ? String(product.section) : "",
      prices: product && Array.isArray(product.prices) ? product.prices : []
    });
  },

  claimSlotProductReward: async function(product) {
    await this.ensureReady();

    var productId = product && product.productId ? String(product.productId) : "";
    if (!productId) {
      return { isSuccess: false, qrPayload: "", error: "Gecersiz urun" };
    }

    var bridge = this;
    var userId = bridge.state.userId;
    await bridge.ensureUserDocument(userId);

    var userRef = bridge.state.db.collection("users").doc(userId);
    var purchasedRef = userRef.collection("purchasedProducts").doc(productId);
    var qrId = bridge.createId();
    var qrPayload = "rocqr:v1:" + userId + ":" + productId + ":" + qrId;
    var qrRef = userRef.collection("QRCodes").doc(qrId);

    return await bridge.state.db.runTransaction(async function(transaction) {
      var purchasedSnapshot = await transaction.get(purchasedRef);
      if (purchasedSnapshot.exists) {
        var purchasedData = purchasedSnapshot.data() || {};
        return {
          isSuccess: true,
          qrPayload: purchasedData.qrPayload || "",
          error: ""
        };
      }

      var priceDetails = [];
      var productName = product.productName || "";
      var productDescription = product.productDescription || "";
      var section = product.section || "";

      transaction.set(purchasedRef, {
        productId: productId,
        productName: productName,
        productDescription: productDescription,
        section: section,
        qrId: qrId,
        qrPayload: qrPayload,
        prices: priceDetails,
        spentSummary: "slot_reward",
        createdAt: bridge.serverTimestamp(),
        status: "purchased",
        source: "slot_reward"
      });

      transaction.set(qrRef, {
        id: qrId,
        productId: productId,
        productName: productName,
        productDescription: productDescription,
        section: section,
        userId: userId,
        payload: qrPayload,
        prices: priceDetails,
        spentSummary: "slot_reward",
        createdAt: bridge.serverTimestamp(),
        status: "active",
        source: "slot_reward"
      });

      return { isSuccess: true, qrPayload: qrPayload, error: "" };
    });
  },

  callFunction: async function(functionName, payload) {
    await this.ensureReady();

    if (!this.state.functions || typeof this.state.functions.httpsCallable !== "function") {
      throw new Error("Firebase Functions SDK is not available.");
    }

    var callable = this.state.functions.httpsCallable(functionName);
    var result = await callable(payload || {});
    return result && result.data ? result.data : {};
  },

  startRun: async function(playEnergyCost) {
    return await this.callFunction("startRun", {
      playEnergyCost: Math.max(0, Math.floor(Number(playEnergyCost) || 0))
    });
  },

  claimRunRewards: async function(payload) {
    return await this.callFunction("claimRunRewards", payload || {});
  },

  claimSpinReward: async function(segmentCount) {
    return await this.callFunction("claimSpinReward", {
      segmentCount: Math.max(0, Math.floor(Number(segmentCount) || 0))
    });
  },

  getShopConfig: async function() {
    return await this.callFunction("getShopConfig", {});
  },

  getActivePurchasedProducts: async function() {
    await this.ensureReady();

    var bridge = this;
    var userRef = bridge.state.db.collection("users").doc(bridge.state.userId);
    var purchasedSnapshot = await userRef.collection("purchasedProducts").get({ source: "server" });
    var productDocs = [];
    purchasedSnapshot.forEach(function(doc) {
      productDocs.push({ id: doc.id, data: doc.data() || {} });
    });

    var products = [];
    for (var i = 0; i < productDocs.length; i++) {
      var productDoc = productDocs[i];
      var productId = productDoc.data.productId || productDoc.id;
      var qrId = productDoc.data.qrId || "";
      var qrPayload = productDoc.data.qrPayload || "";

      if (!qrId) {
        continue;
      }

      var qrSnapshot = await userRef.collection("QRCodes").doc(qrId).get({ source: "server" });
      if (!qrSnapshot.exists) {
        continue;
      }

      var qrData = qrSnapshot.data() || {};
      if (String(qrData.status || "").toLowerCase() !== "active") {
        continue;
      }

      products.push({
        productId: productId,
        qrPayload: qrPayload || qrData.payload || ""
      });
    }

    return { products: products };
  },

  startOperation: function(action) {
    var state = this.state;
    var id = state.nextOperationId++;
    var bridge = this;
    state.operations[id] = {
      status: 0,
      result: "",
      error: ""
    };

    Promise.resolve()
      .then(action)
      .then(function(result) {
        if (!state.operations[id] || state.operations[id].status !== 0) {
          return;
        }

        state.operations[id] = {
          status: 1,
          result: JSON.stringify(result || {}),
          error: ""
        };
      })
      .catch(function(error) {
        if (!state.operations[id] || state.operations[id].status !== 0) {
          return;
        }

        state.operations[id] = {
          status: 2,
          result: "",
          error: RocFirebaseWebGLBridge.getErrorMessage(error)
        };
        console.warn("[RocFirebase] Operation failed:", error);
      });

    return id;
  },

  timestampToMillis: function(value) {
    if (!value) {
      return 0;
    }

    if (typeof value.toMillis === "function") {
      return value.toMillis();
    }

    if (typeof value.seconds === "number") {
      return value.seconds * 1000;
    }

    if (value instanceof Date) {
      return value.getTime();
    }

    return 0;
  },

  createId: function() {
    if (globalThis.crypto && typeof globalThis.crypto.randomUUID === "function") {
      return globalThis.crypto.randomUUID().replace(/-/g, "");
    }

    return "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx".replace(/[x]/g, function() {
      return Math.floor(Math.random() * 16).toString(16);
    });
  },

  getErrorMessage: function(error) {
    if (!error) {
      return "Unknown Firebase error";
    }

    var code = error.code ? String(error.code) + ": " : "";
    var message = error.message || String(error);

    if (error.code === "auth/unauthorized-domain") {
      message += " Current domain " + globalThis.location.hostname +
        " must be added to Firebase Auth authorized domains for project matcha-birdy.";
    }

    return code + message;
  }
  },

  RocFirebase_BeginInitialize: function() {
    RocFirebaseWebGLBridge.initialize().catch(function() {});
  },

  RocFirebase_GetInitState: function() {
    return RocFirebaseWebGLBridge.state.status;
  },

  RocFirebase_GetUserId: function() {
    return RocFirebaseWebGLBridge.makeString(RocFirebaseWebGLBridge.state.userId);
  },

  RocFirebase_GetLastError: function() {
    return RocFirebaseWebGLBridge.makeString(RocFirebaseWebGLBridge.state.error);
  },

  RocFirebase_EnsureUserDocument: function() {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.ensureReady().then(function() {
        return RocFirebaseWebGLBridge.ensureUserDocument(RocFirebaseWebGLBridge.state.userId);
      }).then(function() {
        return { ok: true };
      });
    });
  },

  RocFirebase_ClearCurrentUserData: function() {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.clearCurrentUserData();
    });
  },

  RocFirebase_SyncCurrencyAmount: function(currencyIdPtr, amount) {
    var currencyId = UTF8ToString(currencyIdPtr);
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.syncCurrencyAmount(currencyId, amount);
    });
  },

  RocFirebase_ModifyCurrencyAmount: function(currencyIdPtr, delta) {
    var currencyId = UTF8ToString(currencyIdPtr);
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.modifyCurrencyAmount(currencyId, delta);
    });
  },

  RocFirebase_GetCurrencyAmounts: function() {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.getCurrencyAmounts();
    });
  },

  RocFirebase_GetCurrencyAmount: function(currencyIdPtr) {
    var currencyId = UTF8ToString(currencyIdPtr);
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.getCurrencyAmount(currencyId);
    });
  },

  RocFirebase_GetEnergyAmount: function(maxEnergy) {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.getEnergyAmount(maxEnergy);
    });
  },

  RocFirebase_ClaimDailyEnergy: function(maxEnergy, dailyEnergy, cooldownHours) {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.claimDailyEnergy(maxEnergy, dailyEnergy, cooldownHours);
    });
  },

  RocFirebase_TrySpendEnergy: function(amount) {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.trySpendEnergy(amount);
    });
  },

  RocFirebase_TryUseFreeSpin: function(cooldownHours) {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.tryUseFreeSpin(cooldownHours);
    });
  },

  RocFirebase_TryPurchaseProduct: function(productJsonPtr) {
    var productJson = UTF8ToString(productJsonPtr);
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.tryPurchaseProduct(JSON.parse(productJson));
    });
  },

  RocFirebase_ClaimSlotProductReward: function(productJsonPtr) {
    var productJson = UTF8ToString(productJsonPtr);
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.claimSlotProductReward(JSON.parse(productJson));
    });
  },

  RocFirebase_StartRun: function(playEnergyCost) {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.startRun(playEnergyCost);
    });
  },

  RocFirebase_ClaimRunRewards: function(payloadJsonPtr) {
    var payloadJson = UTF8ToString(payloadJsonPtr);
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.claimRunRewards(JSON.parse(payloadJson));
    });
  },

  RocFirebase_ClaimSpinReward: function(segmentCount) {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.claimSpinReward(segmentCount);
    });
  },

  RocFirebase_GetShopConfig: function() {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.getShopConfig();
    });
  },

  RocFirebase_GetActivePurchasedProducts: function() {
    return RocFirebaseWebGLBridge.startOperation(function() {
      return RocFirebaseWebGLBridge.getActivePurchasedProducts();
    });
  },

  RocFirebase_GetOperationState: function(operationId) {
    var operation = RocFirebaseWebGLBridge.state.operations[operationId];
    return operation ? operation.status : 2;
  },

  RocFirebase_GetOperationResultJson: function(operationId) {
    var operation = RocFirebaseWebGLBridge.state.operations[operationId];
    return RocFirebaseWebGLBridge.makeString(operation ? operation.result : "");
  },

  RocFirebase_GetOperationError: function(operationId) {
    var operation = RocFirebaseWebGLBridge.state.operations[operationId];
    return RocFirebaseWebGLBridge.makeString(operation ? operation.error : "Operation not found.");
  },

  RocFirebase_ReleaseOperation: function(operationId) {
    delete RocFirebaseWebGLBridge.state.operations[operationId];
  }
};

autoAddDeps(RocFirebaseWebGLLibrary, "$RocFirebaseWebGLBridge");
mergeInto(LibraryManager.library, RocFirebaseWebGLLibrary);
