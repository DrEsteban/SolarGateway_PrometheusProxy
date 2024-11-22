using System.ComponentModel.DataAnnotations;

namespace SolarGateway_PrometheusProxy.Support;

/// <summary>
/// Provides conditional validation based on a related property value.
/// </summary>
public class RequiredIfAttribute : ValidationAttribute
{
    /// <summary>
    /// Gets or sets the other property name that will be used during validation.
    /// </summary>
    /// <value>
    /// The other property name.
    /// </value>
    public string DependentPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the other property value that will be relevant for validation.
    /// </summary>
    /// <value>
    /// The other property value.
    /// </value>
    public object? DependentPropertyValue { get; set; }

    [Required]
    public override bool RequiresValidationContext => true;

    public RequiredIfAttribute(string dependentPropertyName, object? dependentPropertyValue)
    {
        this.DependentPropertyName = dependentPropertyName;
        this.DependentPropertyValue = dependentPropertyValue;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        var field = validationContext.ObjectType.GetProperty(this.DependentPropertyName)
            ?? throw new InvalidOperationException($"Dependent property '{this.DependentPropertyName}' not found");
        var dependentValue = field.GetValue(validationContext.ObjectInstance);

        // Check for simple nullability mismatch first
        bool requiredAttributeRequired =
            (dependentValue == null && DependentPropertyValue == null) ||
            (dependentValue != null && DependentPropertyValue != null);
        // Check for equality mismatch
        if (requiredAttributeRequired &&
            dependentValue != null)
        {
            // Try to get type-specific Equals method
            var equalityMethod = field.PropertyType.GetMethod("Equals", [field.PropertyType]);

            // Check for equality using the type-specific Equals method if available
            requiredAttributeRequired =
                (bool?)equalityMethod?.Invoke(dependentValue, [DependentPropertyValue])
                    ?? object.Equals(dependentValue, DependentPropertyValue);
        }

        if (requiredAttributeRequired)
        {
            return new RequiredAttribute().GetValidationResult(value, validationContext);
        }
        return ValidationResult.Success;
    }
}