const crypto = require("crypto");
const { onCall, HttpsError } = require("firebase-functions/v2/https");
const admin = require("firebase-admin");

admin.initializeApp();

const db = admin.firestore();
const Timestamp = admin.firestore.Timestamp;

const USERS_COLLECTION = "users";
const CURRENCIES_COLLECTION = "currencies";
const RUN_SESSIONS_COLLECTION = "runSessions";
const CURRENCY_EVENTS_COLLECTION = "currencyEvents";
const ECONOMY_COLLECTION = "economy";
const REWARD_CONFIG_DOCUMENT = "rewardConfig";
const SHOP_CONFIG_COLLECTION = "ShopConfig";
const SHOP_PRODUCTS_DOCUMENT = "products";
const ENERGY_CURRENCY_ID = "energy";
const CALLABLE_FUNCTION_OPTIONS = {
  cors: [
    "https://the-roc-bayrakli.web.app",
    "https://the-roc-bayrakli.firebaseapp.com",
    /^http:\/\/localhost(:\d+)?$/,
    /^http:\/\/127\.0\.0\.1(:\d+)?$/
  ]
};

const DEFAULT_REWARD_CONFIG = {
  version: 1,
  energy: {
    maxEnergy: 15,
    playCost: 1,
    spinCost: 1
  },
  run: {
    maxCoinPerSecond: 2,
    currencyCapsPerSecond: {
      coin: 2,
      coffee: 2,
      matcha: 2,
      cookie: 2
    }
  },
  SpinRewardAmount_Coin: 20,
  SpinRewardAmount_Coffee: 2,
  SpinRewardAmount_Matcha: 2,
  SpinRewardAmount_Cookie: 2,
  spinRewards: {
    coin: {
      currencyId: "coin",
      amount: 20
    },
    coffee: {
      currencyId: "coffee",
      amount: 2
    },
    matcha: {
      currencyId: "matcha",
      amount: 2
    },
    cookie: {
      currencyId: "cookie",
      amount: 2
    }
  },
  spinSegments: [
    { rewardId: "spin_coin_20_a", currencyId: "coin", amount: 20 },
    { rewardId: "spin_coffee_2_a", currencyId: "coffee", amount: 2 },
    { rewardId: "spin_matcha_2_a", currencyId: "matcha", amount: 2 },
    { rewardId: "spin_cookie_2_a", currencyId: "cookie", amount: 2 },
    { rewardId: "spin_coin_20_b", currencyId: "coin", amount: 20 },
    { rewardId: "spin_coffee_2_b", currencyId: "coffee", amount: 2 },
    { rewardId: "spin_matcha_2_b", currencyId: "matcha", amount: 2 },
    { rewardId: "spin_cookie_2_b", currencyId: "cookie", amount: 2 }
  ]
};

const DEFAULT_SHOP_CONFIG = {
  version: 2,
  products: {
    americano: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "coffee", amount: 10 }
      ]
    },
    cappuccino: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "coffee", amount: 10 }
      ]
    },
    espresso: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "coffee", amount: 10 }
      ]
    },
    filter_coffee: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "coffee", amount: 10 }
      ]
    },
    latte: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "coffee", amount: 10 }
      ]
    },
    flat_white: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "coffee", amount: 10 }
      ]
    },
    cookie: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "cookie", amount: 10 }
      ]
    },
    kruvasan: {
      costs: [
        { currency: "coin", amount: 300 },
        { currency: "cookie", amount: 20 }
      ]
    },
    cheesecake: {
      costs: [
        { currency: "coin", amount: 200 },
        { currency: "cookie", amount: 20 }
      ]
    },
    sandwich: {
      costs: [
        { currency: "coin", amount: 350 },
        { currency: "cookie", amount: 20 }
      ]
    },
    blackberry_matcha: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "matcha", amount: 10 }
      ]
    },
    blueberry_matcha: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "matcha", amount: 10 }
      ]
    },
    mango_matcha: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "matcha", amount: 10 }
      ]
    },
    matcha: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "matcha", amount: 10 }
      ]
    },
    strawberry_matcha: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "matcha", amount: 10 }
      ]
    },
    vanillia_matcha: {
      costs: [
        { currency: "coin", amount: 100 },
        { currency: "matcha", amount: 10 }
      ]
    }
  }
};

exports.startRun = onCall(CALLABLE_FUNCTION_OPTIONS, async (request) => {
  const userId = requireUserId(request);
  const rewardConfig = await ensureRewardConfig();
  const userRef = db.collection(USERS_COLLECTION).doc(userId);
  const energyRef = userRef.collection(CURRENCIES_COLLECTION).doc(ENERGY_CURRENCY_ID);
  const runRef = userRef.collection(RUN_SESSIONS_COLLECTION).doc();
  const now = Timestamp.now();

  return await db.runTransaction(async (transaction) => {
    const energySnapshot = await transaction.get(energyRef);
    const currentEnergy = readAmount(energySnapshot);
    const playCost = getEnergyCost(rewardConfig, "playCost");

    if (currentEnergy < playCost) {
      return {
        isSuccess: false,
        isInsufficient: true,
        runId: "",
        energyBalance: currentEnergy,
        error: "Yetersiz enerji"
      };
    }

    const nextEnergy = currentEnergy - playCost;
    const maxCoinPerSecond = getRunCurrencyCapPerSecond(rewardConfig, "coin");

    transaction.set(energyRef, {
      amount: nextEnergy,
      updatedAt: now
    }, { merge: true });

    transaction.set(runRef, {
      runId: runRef.id,
      source: "run",
      status: "started",
      startedAt: now,
      energyCost: playCost,
      maxCoinPerSecond,
      createdAt: now
    });

    transaction.set(userRef.collection(CURRENCY_EVENTS_COLLECTION).doc(`run_start_${runRef.id}`), {
      eventId: `run_start_${runRef.id}`,
      source: "run_start",
      runId: runRef.id,
      currencyId: ENERGY_CURRENCY_ID,
      amount: -playCost,
      balanceAfter: nextEnergy,
      createdAt: now
    });

    return {
      isSuccess: true,
      isInsufficient: false,
      runId: runRef.id,
      energyBalance: nextEnergy,
      maxCoinPerSecond,
      error: ""
    };
  });
});

exports.claimRunRewards = onCall(CALLABLE_FUNCTION_OPTIONS, async (request) => {
  const data = request.data || {};
  const userId = requireUserId(request);
  const runId = typeof data?.runId === "string" ? data.runId.trim() : "";

  if (!runId) {
    throw new HttpsError("invalid-argument", "runId zorunlu.");
  }

  const requestedRewards = aggregateRewards(data?.rewards);
  const rewardConfig = await ensureRewardConfig();
  const userRef = db.collection(USERS_COLLECTION).doc(userId);
  const runRef = userRef.collection(RUN_SESSIONS_COLLECTION).doc(runId);
  const now = Timestamp.now();

  return await db.runTransaction(async (transaction) => {
    const runSnapshot = await transaction.get(runRef);

    if (!runSnapshot.exists) {
      return failedRewardClaim("Run oturumu bulunamadi");
    }

    const runData = runSnapshot.data() || {};
    if (runData.status === "claimed") {
      return failedRewardClaim("Run odulu daha once alinmis");
    }

    if (runData.status !== "started") {
      return failedRewardClaim("Run oturumu aktif degil");
    }

    const startedAt = runData.startedAt;
    if (!startedAt || typeof startedAt.toMillis !== "function") {
      return failedRewardClaim("Run baslangic zamani gecersiz");
    }

    const durationSeconds = Math.max(0, (now.toMillis() - startedAt.toMillis()) / 1000);
    const grantEntries = buildRunGrantEntries(rewardConfig, requestedRewards, durationSeconds);
    const balances = {};

    for (const entry of grantEntries) {
      if (entry.grantedAmount <= 0) {
        entry.balance = 0;
        continue;
      }

      const currencyRef = userRef.collection(CURRENCIES_COLLECTION).doc(entry.currencyId);
      const currencySnapshot = await transaction.get(currencyRef);
      balances[entry.currencyId] = readAmount(currencySnapshot);
    }

    const requestedMap = {};
    const grantedMap = {};

    for (const entry of grantEntries) {
      requestedMap[entry.currencyId] = entry.requestedAmount;
      grantedMap[entry.currencyId] = entry.grantedAmount;

      const currentBalance = balances[entry.currencyId] || 0;
      const nextBalance = clampInt(currentBalance + entry.grantedAmount);
      entry.balance = nextBalance;

      if (entry.grantedAmount > 0) {
        transaction.set(userRef.collection(CURRENCIES_COLLECTION).doc(entry.currencyId), {
          amount: nextBalance,
          updatedAt: now
        }, { merge: true });
      }

      transaction.set(userRef.collection(CURRENCY_EVENTS_COLLECTION).doc(`run_${runId}_${entry.currencyId}`), {
        eventId: `run_${runId}_${entry.currencyId}`,
        source: "run",
        sourceDetail: "gameplay_run",
        runId,
        currencyId: entry.currencyId,
        requestedAmount: entry.requestedAmount,
        amount: entry.grantedAmount,
        balanceAfter: nextBalance,
        durationSeconds,
        createdAt: now
      });
    }

    transaction.set(runRef, {
      status: "claimed",
      claimedAt: now,
      durationSeconds,
      requestedRewards: requestedMap,
      grantedRewards: grantedMap
    }, { merge: true });

    return {
      isSuccess: true,
      runId,
      durationSeconds,
      grants: grantEntries,
      error: ""
    };
  });
});

exports.claimSpinReward = onCall(CALLABLE_FUNCTION_OPTIONS, async (request) => {
  const userId = requireUserId(request);
  const rewardConfig = await ensureRewardConfig();
  const segments = getSpinSegments(rewardConfig);

  if (segments.length === 0) {
    throw new HttpsError("failed-precondition", "Spin reward config bos.");
  }

  const selectedIndex = crypto.randomInt(0, segments.length);
  const selectedSegment = segments[selectedIndex];
  const spinCost = getEnergyCost(rewardConfig, "spinCost");
  const now = Timestamp.now();
  const userRef = db.collection(USERS_COLLECTION).doc(userId);
  const energyRef = userRef.collection(CURRENCIES_COLLECTION).doc(ENERGY_CURRENCY_ID);
  const rewardCurrencyRef = userRef.collection(CURRENCIES_COLLECTION).doc(selectedSegment.currencyId);
  const eventRef = userRef.collection(CURRENCY_EVENTS_COLLECTION).doc();

  return await db.runTransaction(async (transaction) => {
    const energySnapshot = await transaction.get(energyRef);
    const currentEnergy = readAmount(energySnapshot);

    if (currentEnergy < spinCost) {
      return {
        isSuccess: false,
        isInsufficient: true,
        energyBalance: currentEnergy,
        segmentIndex: selectedIndex,
        rewardId: selectedSegment.rewardId,
        currencyId: selectedSegment.currencyId,
        amount: 0,
        balance: 0,
        error: "Yetersiz enerji"
      };
    }

    const rewardSnapshot = await transaction.get(rewardCurrencyRef);
    const currentRewardBalance = readAmount(rewardSnapshot);
    const nextRewardBalance = clampInt(currentRewardBalance + selectedSegment.amount);
    const nextEnergy = currentEnergy - spinCost;

    transaction.set(energyRef, {
      amount: nextEnergy,
      updatedAt: now
    }, { merge: true });

    transaction.set(rewardCurrencyRef, {
      amount: nextRewardBalance,
      updatedAt: now
    }, { merge: true });

    transaction.set(eventRef, {
      eventId: eventRef.id,
      source: "spin",
      sourceDetail: "spin_reward",
      rewardId: selectedSegment.rewardId,
      segmentIndex: selectedIndex,
      currencyId: selectedSegment.currencyId,
      requestedAmount: selectedSegment.amount,
      amount: selectedSegment.amount,
      balanceAfter: nextRewardBalance,
      energyCost: spinCost,
      energyBalanceAfter: nextEnergy,
      createdAt: now
    });

    return {
      isSuccess: true,
      isInsufficient: false,
      energyBalance: nextEnergy,
      segmentIndex: selectedIndex,
      rewardId: selectedSegment.rewardId,
      currencyId: selectedSegment.currencyId,
      amount: selectedSegment.amount,
      balance: nextRewardBalance,
      error: ""
    };
  });
});

exports.getShopConfig = onCall(CALLABLE_FUNCTION_OPTIONS, async (request) => {
  requireUserId(request);
  const shopConfig = await ensureShopConfig();

  return {
    isSuccess: true,
    products: buildShopProductsResponse(shopConfig),
    error: ""
  };
});

exports.purchaseProduct = onCall(CALLABLE_FUNCTION_OPTIONS, async (request) => {
  const data = request.data || {};
  const userId = requireUserId(request);
  const productId = normalizeProductId(data?.productId);

  if (!productId) {
    throw new HttpsError("invalid-argument", "productId zorunlu.");
  }

  const shopConfig = await ensureShopConfig();
  const shopProduct = getShopProduct(shopConfig, productId);
  const requestProduct = getRequestProduct(data, productId);
  const product = requestProduct || shopProduct;

  if (!product) {
    return failedPurchase("Urun bulunamadi");
  }

  const totalCostByCurrency = aggregatePrices(getRawProductCosts(product));
  const priceDetails = buildPriceDetails(totalCostByCurrency);

  if (priceDetails.length === 0) {
    return failedPurchase("Gecersiz fiyatlandirma");
  }

  const now = Timestamp.now();
  const userRef = db.collection(USERS_COLLECTION).doc(userId);
  const purchasedRef = userRef.collection("purchasedProducts").doc(productId);
  const qrRef = userRef.collection("QRCodes").doc();
  const qrPayload = buildQrPayload(userId, productId, qrRef.id);
  const spentSummary = formatPriceSummary(totalCostByCurrency);

  return await db.runTransaction(async (transaction) => {
    const purchasedSnapshot = await transaction.get(purchasedRef);
    if (purchasedSnapshot.exists) {
      return failedPurchase("Urun daha once alinmis");
    }

    const balances = {};
    const currencyIds = Object.keys(totalCostByCurrency).sort();
    for (const currencyId of currencyIds) {
      const currencyRef = userRef.collection(CURRENCIES_COLLECTION).doc(currencyId);
      const currencySnapshot = await transaction.get(currencyRef);
      const currentBalance = readAmount(currencySnapshot);

      if (currentBalance < totalCostByCurrency[currencyId]) {
        return failedPurchase("Yetersiz bakiye");
      }

      balances[currencyId] = currentBalance;
    }

    for (const currencyId of currencyIds) {
      const spentAmount = totalCostByCurrency[currencyId];
      const nextBalance = clampInt(balances[currencyId] - spentAmount);

      transaction.set(userRef.collection(CURRENCIES_COLLECTION).doc(currencyId), {
        amount: nextBalance,
        updatedAt: now
      }, { merge: true });

      transaction.set(userRef.collection(CURRENCY_EVENTS_COLLECTION).doc(), {
        source: "store_purchase",
        sourceDetail: "product_purchase",
        productId,
        qrId: qrRef.id,
        currencyId,
        amount: -spentAmount,
        balanceAfter: nextBalance,
        createdAt: now
      });
    }

    const productPayload = {
      productId,
      qrId: qrRef.id,
      qrPayload,
      costs: priceDetails,
      spentSummary,
      createdAt: now,
      status: "purchased",
      source: "store_purchase"
    };

    transaction.set(purchasedRef, productPayload);

    transaction.set(qrRef, {
      id: qrRef.id,
      productId,
      userId,
      payload: qrPayload,
      costs: priceDetails,
      spentSummary,
      createdAt: now,
      status: "active",
      source: "store_purchase"
    });

    return {
      isSuccess: true,
      qrPayload,
      costs: priceDetails,
      spentSummary,
      error: ""
    };
  });
});

async function ensureRewardConfig() {
  const configRef = db.collection(ECONOMY_COLLECTION).doc(REWARD_CONFIG_DOCUMENT);
  const snapshot = await configRef.get();

  if (snapshot.exists) {
    return snapshot.data() || {};
  }

  await configRef.set({
    ...DEFAULT_REWARD_CONFIG,
    createdAt: Timestamp.now(),
    updatedAt: Timestamp.now()
  });

  return DEFAULT_REWARD_CONFIG;
}

async function getRewardConfig(transaction) {
  const configRef = db.collection(ECONOMY_COLLECTION).doc(REWARD_CONFIG_DOCUMENT);
  const snapshot = await transaction.get(configRef);

  if (snapshot.exists) {
    return snapshot.data() || {};
  }

  transaction.set(configRef, {
    ...DEFAULT_REWARD_CONFIG,
    createdAt: Timestamp.now(),
    updatedAt: Timestamp.now()
  });

  return DEFAULT_REWARD_CONFIG;
}

async function ensureShopConfig() {
  const configRef = db.collection(SHOP_CONFIG_COLLECTION).doc(SHOP_PRODUCTS_DOCUMENT);
  const snapshot = await configRef.get();

  if (!snapshot.exists) {
    const products = normalizeShopProducts(null);
    const config = {
      version: DEFAULT_SHOP_CONFIG.version,
      products,
      createdAt: Timestamp.now(),
      updatedAt: Timestamp.now()
    };

    await configRef.set(config);

    return config;
  }

  const rawConfig = snapshot.data() || {};
  const products = normalizeShopProducts(rawConfig.products);

  const normalizedConfig = {
    version: DEFAULT_SHOP_CONFIG.version,
    products
  };

  await configRef.set({
    ...normalizedConfig,
    createdAt: rawConfig.createdAt || Timestamp.now(),
    updatedAt: Timestamp.now()
  });

  return normalizedConfig;
}

function buildShopProductsResponse(config) {
  const products = config && typeof config.products === "object" ? config.products : {};
  return Object.keys(products)
    .sort()
    .map((productId) => getShopProduct(config, productId))
    .filter(Boolean);
}

function normalizeShopProducts(rawProducts) {
  const products = {};
  const sourceProducts = rawProducts && typeof rawProducts === "object" ? rawProducts : {};
  const productIds = Object.keys(DEFAULT_SHOP_CONFIG.products);

  for (const rawProductId of productIds) {
    const productId = normalizeProductId(rawProductId);
    if (!productId) {
      continue;
    }

    const sourceProduct = sourceProducts[rawProductId];
    const defaultProduct = DEFAULT_SHOP_CONFIG.products[productId];
    const cost = buildPriceDetails(aggregatePrices(
      getRawProductCosts(sourceProduct).length > 0
        ? getRawProductCosts(sourceProduct)
        : getRawProductCosts(defaultProduct)
    ));

    if (cost.length > 0) {
      products[productId] = {
        id: productId,
        cost
      };
    }
  }

  return products;
}

function getShopProduct(config, productId) {
  const products = config && typeof config.products === "object" ? config.products : {};
  const rawProduct = products[productId];

  if (!rawProduct || typeof rawProduct !== "object") {
    return null;
  }

  const normalizedProductId = normalizeProductId(productId);
  if (!normalizedProductId) {
    return null;
  }

  const costs = buildPriceDetails(aggregatePrices(getRawProductCosts(rawProduct)));
  if (costs.length === 0) {
    return null;
  }

  return {
    productId: normalizedProductId,
    costs,
    prices: costs
  };
}

function getRequestProduct(data, productId) {
  const requestProductId = normalizeProductId(data?.productId);
  if (!requestProductId || requestProductId !== productId) {
    return null;
  }

  const costs = buildPriceDetails(aggregatePrices(data?.prices || data?.costs));
  if (costs.length === 0) {
    return null;
  }

  return {
    productId,
    productName: typeof data?.productName === "string" && data.productName
      ? data.productName
      : productId,
    productDescription: typeof data?.productDescription === "string"
      ? data.productDescription
      : "",
    section: typeof data?.section === "string" ? data.section : "",
    costs,
    prices: costs
  };
}

function getRawProductCosts(rawProduct) {
  if (!rawProduct || typeof rawProduct !== "object") {
    return [];
  }

  if (Array.isArray(rawProduct.costs)) {
    return rawProduct.costs;
  }

  if (Array.isArray(rawProduct.cost)) {
    return rawProduct.cost;
  }

  if (Array.isArray(rawProduct.prices)) {
    return rawProduct.prices;
  }

  return [];
}

function aggregatePrices(rawPrices) {
  const totalCostByCurrency = {};
  if (!Array.isArray(rawPrices)) {
    return totalCostByCurrency;
  }

  for (const rawPrice of rawPrices) {
    const currencyId = normalizeCurrencyId(rawPrice && (rawPrice.currency || rawPrice.currencyId));
    const amount = clampInt(rawPrice && rawPrice.amount, 1000000000);

    if (!currencyId || currencyId === ENERGY_CURRENCY_ID || amount <= 0) {
      continue;
    }

    totalCostByCurrency[currencyId] = (totalCostByCurrency[currencyId] || 0) + amount;
  }

  return totalCostByCurrency;
}

function buildPriceDetails(totalCostByCurrency) {
  if (!totalCostByCurrency || typeof totalCostByCurrency !== "object") {
    return [];
  }

  return Object.keys(totalCostByCurrency)
    .map((currencyId) => ({
      currency: currencyId,
      amount: clampInt(totalCostByCurrency[currencyId], 1000000000)
    }))
    .filter((price) => price.amount > 0);
}

function formatPriceSummary(totalCostByCurrency) {
  return buildPriceDetails(totalCostByCurrency)
    .map((price) => `${price.amount} ${price.currency}`)
    .join(", ");
}

function failedPurchase(error) {
  return {
    isSuccess: false,
    qrPayload: "",
    costs: [],
    spentSummary: "",
    error
  };
}

function buildQrPayload(userId, productId, qrId) {
  return `rocqr:v1:${userId}:${productId}:${qrId}`;
}

function requireUserId(context) {
  if (!context.auth || !context.auth.uid) {
    throw new HttpsError("unauthenticated", "Firebase auth gerekli.");
  }

  return context.auth.uid;
}

function readAmount(snapshot) {
  if (!snapshot || !snapshot.exists) {
    return 0;
  }

  return clampInt(snapshot.get("amount"));
}

function clampInt(value, maxValue = Number.MAX_SAFE_INTEGER) {
  const parsed = Math.floor(Number(value) || 0);
  return Math.max(0, Math.min(maxValue, parsed));
}

function getEnergyCost(config, key) {
  const energyConfig = config && typeof config.energy === "object" ? config.energy : {};
  const fallback = DEFAULT_REWARD_CONFIG.energy[key] || 1;
  return clampInt(energyConfig[key] ?? fallback, 15);
}

function getRunCurrencyCapPerSecond(config, currencyId) {
  const runConfig = config && typeof config.run === "object" ? config.run : {};
  const caps = runConfig.currencyCapsPerSecond && typeof runConfig.currencyCapsPerSecond === "object"
    ? runConfig.currencyCapsPerSecond
    : DEFAULT_REWARD_CONFIG.run.currencyCapsPerSecond;

  if (currencyId === "coin") {
    return clampInt(runConfig.maxCoinPerSecond ?? caps.coin ?? 2, 100);
  }

  return clampInt(caps[currencyId] ?? 0, 100);
}

function aggregateRewards(rawRewards) {
  const rewards = {};
  const entries = Array.isArray(rawRewards)
    ? rawRewards
    : Object.entries(rawRewards || {}).map(([currencyId, amount]) => ({ currencyId, amount }));

  for (const reward of entries) {
    const currencyId = normalizeCurrencyId(reward && reward.currencyId);
    if (!currencyId || currencyId === ENERGY_CURRENCY_ID) {
      continue;
    }

    const amount = clampInt(reward.amount, 1000000);
    if (amount <= 0) {
      continue;
    }

    rewards[currencyId] = (rewards[currencyId] || 0) + amount;
  }

  return rewards;
}

function buildRunGrantEntries(config, requestedRewards, durationSeconds) {
  return Object.keys(requestedRewards)
    .sort()
    .map((currencyId) => {
      const requestedAmount = requestedRewards[currencyId];
      const capPerSecond = getRunCurrencyCapPerSecond(config, currencyId);
      const maxAllowed = Math.floor(durationSeconds * capPerSecond);
      const grantedAmount = Math.min(requestedAmount, Math.max(0, maxAllowed));

      return {
        currencyId,
        requestedAmount,
        grantedAmount,
        balance: 0,
        capPerSecond,
        maxAllowed
      };
    });
}

function failedRewardClaim(error) {
  return {
    isSuccess: false,
    runId: "",
    durationSeconds: 0,
    grants: [],
    error
  };
}

function getSpinSegments(config) {
  const configuredSegments = Array.isArray(config && config.spinSegments)
    ? config.spinSegments
    : DEFAULT_REWARD_CONFIG.spinSegments;

  return configuredSegments
    .map((segment, index) => {
      const currencyId = normalizeCurrencyId(segment && segment.currencyId);
      const amount = getSpinRewardAmount(config, currencyId, segment && segment.amount);

      if (!currencyId || amount <= 0 || currencyId === ENERGY_CURRENCY_ID) {
        return null;
      }

      return {
        rewardId: typeof segment.rewardId === "string" && segment.rewardId
          ? segment.rewardId
          : `spin_${currencyId}_${amount}_${index}`,
        currencyId,
        amount
      };
    })
    .filter(Boolean);
}

function getSpinRewardAmount(config, currencyId, segmentAmount) {
  const titleCurrencyId = toTitleCase(currencyId);
  const explicitField = titleCurrencyId ? `SpinRewardAmount_${titleCurrencyId}` : "";

  if (explicitField && Number.isFinite(Number(config && config[explicitField]))) {
    return clampInt(config[explicitField], 1000000);
  }

  const spinRewards = config && typeof config.spinRewards === "object"
    ? config.spinRewards
    : DEFAULT_REWARD_CONFIG.spinRewards;
  const spinReward = spinRewards[currencyId];

  if (Number.isFinite(Number(spinReward))) {
    return clampInt(spinReward, 1000000);
  }

  if (spinReward && Number.isFinite(Number(spinReward.amount))) {
    return clampInt(spinReward.amount, 1000000);
  }

  if (Number.isFinite(Number(segmentAmount))) {
    return clampInt(segmentAmount, 1000000);
  }

  const fallback = DEFAULT_REWARD_CONFIG.spinRewards[currencyId];
  return fallback ? clampInt(fallback.amount, 1000000) : 0;
}

function normalizeCurrencyId(value) {
  if (typeof value !== "string") {
    return "";
  }

  const normalized = value.trim().toLowerCase();
  return /^[a-z0-9_-]{1,40}$/.test(normalized) ? normalized : "";
}

function normalizeProductId(value) {
  if (typeof value !== "string") {
    return "";
  }

  const normalized = value.trim().toLowerCase();
  return /^[a-z0-9_-]{1,80}$/.test(normalized) ? normalized : "";
}

function toTitleCase(value) {
  if (!value) {
    return "";
  }

  return value.charAt(0).toUpperCase() + value.slice(1);
}
