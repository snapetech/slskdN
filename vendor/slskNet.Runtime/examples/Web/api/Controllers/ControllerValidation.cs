namespace WebAPI.Controllers
{
    internal static class ControllerValidation
    {
        public static bool IsMissing(string value)
            => string.IsNullOrWhiteSpace(value);
    }
}
