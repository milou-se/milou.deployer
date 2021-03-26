using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Milou.Deployer.Web.IisHost.Areas.Targets.Controllers
{
    public class ValueObjectModelBinder :IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var tryParseMethod = bindingContext.ModelType
                .GetMethods().SingleOrDefault(m => m.IsStatic && m.Name == "TryParse");


            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }


            if (tryParseMethod is { })
            {
                var parsed = (bool) tryParseMethod.Invoke(null, new object[] {valueProviderResult.FirstValue});
            }

            throw new NotImplementedException();
        }
    }
}