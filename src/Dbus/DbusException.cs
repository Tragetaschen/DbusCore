﻿using System;

namespace Dbus
{
    public class DbusException : Exception
    {
        public DbusException(string errorName, string errorMessage)
            : base($"[{errorName}] {errorMessage}")
        {
            ErrorName = errorName;
            ErrorMessage = errorMessage;
        }

        public string ErrorName { get; }
        public string ErrorMessage { get; }
    }
}
