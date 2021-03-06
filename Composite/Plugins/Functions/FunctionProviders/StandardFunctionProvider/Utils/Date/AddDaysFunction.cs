﻿using System;
using Composite.Functions;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Composite.Plugins.Functions.FunctionProviders.StandardFunctionProvider.Foundation;
using Composite.Core.ResourceSystem;

namespace Composite.Plugins.Functions.FunctionProviders.StandardFunctionProvider.Utils.Date
{
    internal sealed class AddDaysFunction : StandardFunctionBase
    {
        public AddDaysFunction(EntityTokenFactory entityTokenFactory)
            : base("AddDays", "Composite.Utils.Date", typeof(DateTime), entityTokenFactory)
        {
        }


        protected override IEnumerable<StandardFunctionParameterProfile> StandardFunctionParameterProfiles
        {
            get
            {
                WidgetFunctionProvider textboxWidget = StandardWidgetFunctions.IntegerTextBoxWidget;

                yield return new StandardFunctionParameterProfile(
                    "DaysToAdd", typeof(double), true, new ConstantValueProvider((double)0), textboxWidget);
            }
        }


        public override object Execute(ParameterList parameters, FunctionContextContainer context)
        {
            double daysToAdd = parameters.GetParameter<double>("DaysToAdd");
            return DateTime.Now.AddDays(daysToAdd); ;
        }
    }
}
