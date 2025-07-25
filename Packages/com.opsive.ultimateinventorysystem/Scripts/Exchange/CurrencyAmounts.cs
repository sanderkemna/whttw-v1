/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.Registers;
using Opsive.UltimateInventorySystem.Storage;
using Opsive.UltimateInventorySystem.Utility;
using UnityEngine;

namespace Opsive.UltimateInventorySystem
{
    public interface ITypeConverterWithDatabase
    {
        void SetInventorySystemDatabase(InventorySystemDatabase inventorySystemDatabase);
    }
}

namespace Opsive.UltimateInventorySystem.Exchange
{
    using Opsive.UltimateInventorySystem.Core.DataStructures;

    /// <summary>
    /// An array of currency amounts.
    /// </summary>
    [System.Serializable]
    [TypeConverter(typeof(CurrencyAmountsTypeConverter))]
    public class CurrencyAmounts : ObjectAmounts<Currency, CurrencyAmount>
    {
        public CurrencyAmounts() : base()
        { }

        public CurrencyAmounts(CurrencyAmount[] array) : base(array)
        { }

        public static implicit operator CurrencyAmount[](CurrencyAmounts x) => x?.m_Array;
        public static implicit operator CurrencyAmounts(CurrencyAmount[] x) => new CurrencyAmounts(x);
    }
    
    // This is a class used to convert a string representation for CurrencyAmounts into a CurrencyAmounts object.
    // Used mostly for Importing CSV exported databases. It works automatically because TypeDescriptor.GetConverter(typeof CurrencyAmounts) will return this class.
     public class CurrencyAmountsTypeConverter : TypeConverter, ITypeConverterWithDatabase
     {
         private InventorySystemDatabase m_InventorySystemDatabase;

         public CurrencyAmountsTypeConverter()
         {
             InventoryDatabaseUtility.TryGetMainInventorySystemDatabase(out m_InventorySystemDatabase);
             SetInventorySystemDatabase(m_InventorySystemDatabase);
         }
         
         public void SetInventorySystemDatabase(InventorySystemDatabase inventorySystemDatabase)
         {
             m_InventorySystemDatabase = inventorySystemDatabase;
         }
        
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (m_InventorySystemDatabase == null)
            {
                return false;
            }
            
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string stringValue = value as string;

            if (string.IsNullOrEmpty(stringValue))
            {
                return base.ConvertFrom(context, culture, value);
            }

            // Example of string : "Gold x1 | Silver x60 " or "Bronze x75 "
            string regexPattern = @"([a-zA-Z]+)\s*x(\d+)";
            Regex regex = new Regex(regexPattern);
            MatchCollection matches = regex.Matches(stringValue);

            var currencyAmountArray = new CurrencyAmount[matches.Count];

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                string currencyName = match.Groups[1].Value;
                int amountValue = int.Parse(match.Groups[2].Value);

                var currency = m_InventorySystemDatabase.Get<Currency>(currencyName);
                currencyAmountArray[i] = new CurrencyAmount(amountValue, currency);
                
            }
            
            return new CurrencyAmounts(currencyAmountArray);
        }
    }
}