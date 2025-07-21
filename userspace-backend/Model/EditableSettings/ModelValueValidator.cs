namespace userspace_backend.Model.EditableSettings
{
    public interface IModelValueValidator<T>
    {
        bool Validate(T value);
    }

    public class DefaultModelValueValidator<T> : IModelValueValidator<T>
    {
        public const string AllChangeInvalidDIKey = nameof(AllChangeInvalidDIKey);

        public bool Validate(T value)
        {
            return true;
        }
    }

    public class AllChangeInvalidValueValidator<T> : IModelValueValidator<T>
    {
        public bool Validate(T value) => false;
    }
}
