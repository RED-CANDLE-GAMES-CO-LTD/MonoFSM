using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Primitive
{
    [Serializable]
    public struct SerializableDateTime : IComparable<SerializableDateTime>
    {
        [SerializeField] [HideInInspector] private long m_ticks;
        private bool initialized;
        private DateTime m_dateTime;
        [ShowInInspector] public string DateTimeString => DateTime.ToString("yyyy/MM/dd HH:mm:ss");

        public void UpdateTime()
        {
            m_ticks = DateTime.Now.Ticks;
            m_dateTime = DateTime.Now;
        }

        public DateTime DateTime
        {
            get
            {
                if (!initialized)
                {
                    m_dateTime = new DateTime(m_ticks);
                    initialized = true;
                }

                return m_dateTime;
            }
        }

        public SerializableDateTime(DateTime dateTime)
        {
            m_ticks = dateTime.Ticks;
            m_dateTime = dateTime;
            initialized = true;
        }

        public int CompareTo(SerializableDateTime other)
        {
            return m_ticks.CompareTo(other.m_ticks);
        }
    }
}