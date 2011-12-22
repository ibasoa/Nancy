namespace Nancy.Validation.Fluent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FV = FluentValidation;

    public class FluentValidationValidator : IValidator
    {
        private static readonly Dictionary<Type, Func<string, FV.Validators.IPropertyValidator, IFluentAdapter>> factories = new Dictionary<Type, Func<string, FV.Validators.IPropertyValidator, IFluentAdapter>>
        {
            { typeof(FV.Validators.RegularExpressionValidator), (memberName, propertyValdiator) => new RegexAdapter(memberName, (FV.Validators.RegularExpressionValidator)propertyValdiator) }
        };

        private readonly FV.IValidator validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentValidationValidator"/> class for the
        /// specified <see cref="FluentValidation.IValidator"/>.
        /// </summary>
        /// <param name="validator"></param>
        public FluentValidationValidator(FV.IValidator validator)
        {
            this.validator = validator;
        }

        /// <summary>
        /// Gets the description of the validator.
        /// </summary>
        /// <value>A <see cref="ValidationDescriptor"/> instance.</value>
        public ValidationDescriptor Description
        {
            get { return CreateDescriptor(); }
        }

        /// <summary>
        /// Validates the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>A ValidationResult with the result of the validation.</returns>
        public ValidationResult Validate(object instance)
        {
            var result =
                this.validator.Validate(instance);

            var errors =
                GetErrors(result);

            return new ValidationResult(errors);
        }

        /// <summary>
        /// Creates the descriptor.
        /// </summary>
        /// <returns></returns>
        private ValidationDescriptor CreateDescriptor()
        {
            var fluentDescriptor =
                this.validator.CreateDescriptor();

            var rules = new List<ValidationRule>();

            var membersWithValidators = fluentDescriptor.GetMembersWithValidators();
            foreach (var memberValidators in membersWithValidators)
            {
                foreach (var validator in memberValidators)
                {
                    rules.AddRange(GetValidationRule(memberValidators.Key, validator));
                }
            }

            return new ValidationDescriptor(rules);
        }

        private static IEnumerable<ValidationRule> GetValidationRule(string propertyName, FV.Validators.IPropertyValidator propertyValidator)
        {
            Func<string, FV.Validators.IPropertyValidator, IFluentAdapter> factory;
            if (!factories.TryGetValue(propertyValidator.GetType(), out factory))
            {
                factory = (a, d) => new FluentAdapter("Custom", propertyName, propertyValidator);
            }

            return factory(propertyName, propertyValidator).GetRules();
        }

        private static IEnumerable<ValidationError> GetErrors(FluentValidation.Results.ValidationResult results)
        {
            if(results.IsValid)
            {
                return Enumerable.Empty<ValidationError>();
            }

            return results.Errors.Select(error => new ValidationError(new[] {error.PropertyName}, s => error.ErrorMessage));
        }
    }
}