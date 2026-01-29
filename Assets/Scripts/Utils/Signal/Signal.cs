using System;
using System.Linq;

namespace Utils.Signal
{
    public class BaseSignal : ISignal
    {
        public event Action<ISignal, object[]> BaseListener;
        private event Action<ISignal> GenericListener;

        private string _hash;

        public void Subscribe(Action<ISignal, object[]> callback)
        {
            if (BaseListener == null || !BaseListener.GetInvocationList().Contains(callback))
            {
                BaseListener += callback;
            }
        }

        public void Subscribe(Action<ISignal> action)
        {
            GenericListener += action;
        }

        public void Unsubscribe(Action<ISignal> action)
        {
            GenericListener -= action;
        }

        public void Invoke(object[] args)
        {
            BaseListener?.Invoke(this, args);
            GenericListener?.Invoke(this);
        }
    }

    public class Signal : BaseSignal
    {
        private event Action _callback;

        public void Subscribe(Action handler)
        {
            _callback += handler;
        }

        public void Unsubscribe(Action handler)
        {
            _callback -= handler;
        }

        public void UnsubscribeAll()
        {
            _callback = null;
        }

        public void Invoke()
        {
            _callback?.Invoke();
        }
    }

    public class Signal<T> : BaseSignal
    {
        private event Action<T> _callback;

        public void Subscribe(Action<T> handler)
        {
            _callback += handler;
        }

        public void Unsubscribe(Action<T> handler)
        {
            _callback -= handler;
        }

        public void UnsubscribeAll()
        {
            _callback = null;
        }

        public void Invoke(T arg)
        {
            _callback?.Invoke(arg);
        }
    }

    public class Signal<TArg1, TArg2> : BaseSignal
    {
        private event Action<TArg1, TArg2> _callback;

        public void Subscribe(Action<TArg1, TArg2> handler)
        {
            _callback += handler;
        }

        public void Unsubscribe(Action<TArg1, TArg2> handler)
        {
            _callback -= handler;
        }

        public void UnsubscribeAll()
        {
            _callback = null;
        }

        public void Invoke(TArg1 arg1, TArg2 arg2)
        {
            _callback?.Invoke(arg1, arg2);
        } 
    }

    public class Signal<TArg1, TArg2, TArg3> : BaseSignal
    {
        private event Action<TArg1, TArg2, TArg3> _callback;

        public void Subscribe(Action<TArg1, TArg2, TArg3> handler)
        {
            _callback += handler;
        }

        public void Unsubscribe(Action<TArg1, TArg2, TArg3> handler)
        {
            _callback -= handler;
        }

        public void UnsubscribeAll()
        {
            _callback = null;
        }

        public void Invoke(TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            _callback?.Invoke(arg1, arg2, arg3);
        } 
    }
}