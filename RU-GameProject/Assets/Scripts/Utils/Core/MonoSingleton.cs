using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RU.Core.Utils.Core
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        protected static T instance;

        public static T Instance
        {
            get { return instance; }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = (T) this;
            }
            else
            {
                Debug.LogError("Get a second instance of this class" + this.GetType());
            }
        }
    }
}