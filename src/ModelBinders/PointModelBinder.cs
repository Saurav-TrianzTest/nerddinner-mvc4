using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NetTopologySuite.Geometries;

namespace NerdDinner.ModelBinders
{
    /// <summary>
    /// Binds a "latitude,longitude" string to an NTS Point (SRID 4326, X = longitude, Y = latitude).
    /// </summary>
    public class PointModelBinder : IModelBinder
    {
        public static readonly GeometryFactory GeometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                // Empty value means no location was supplied.
                return Task.CompletedTask;
            }

            var parts = value.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
                || latitude < -90 || latitude > 90
                || longitude < -180 || longitude > 180)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                    "Location must be in the format 'latitude,longitude'.");
                return Task.CompletedTask;
            }

            //4326 format puts LONGITUDE first then LATITUDE
            var point = GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));
            bindingContext.Result = ModelBindingResult.Success(point);
            return Task.CompletedTask;
        }
    }

    public class PointModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.Metadata.ModelType == typeof(Point))
            {
                return new PointModelBinder();
            }

            return null;
        }
    }
}
