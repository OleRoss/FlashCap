#if !NET7_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Indicates that the specified method requires the ability to generate new code at runtime,
    /// for example through <see cref="Reflection"/>.
    /// </summary>
    /// <remarks>
    /// This allows tools to understand which methods are unsafe to call when compiling ahead of time.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresDynamicCodeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresDynamicCodeAttribute"/> class
        /// with the specified message.
        /// </summary>
        /// <param name="message">
        /// A message that contains information about the usage of dynamic code.
        /// </param>
        public RequiresDynamicCodeAttribute(string message)
        {
            Message = message;
        }

        /// <summary>
        /// When set to true, indicates that the annotation should not apply to static members.
        /// </summary>
        public bool ExcludeStatics { get; set; }

        /// <summary>
        /// Gets a message that contains information about the usage of dynamic code.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets or sets an optional URL that contains more information about the method,
        /// why it requires dynamic code, and what options a consumer has to deal with it.
        /// </summary>
        public string? Url { get; set; }
    }
}
#endif

#if FLASHCAP_MEDIAFOUNDATION && !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}

namespace System.Runtime.Versioning
{
    [AttributeUsage(
        AttributeTargets.Assembly |
        AttributeTargets.Class |
        AttributeTargets.Constructor |
        AttributeTargets.Delegate |
        AttributeTargets.Enum |
        AttributeTargets.Event |
        AttributeTargets.Field |
        AttributeTargets.Interface |
        AttributeTargets.Method |
        AttributeTargets.Module |
        AttributeTargets.Property |
        AttributeTargets.Struct,
        AllowMultiple = true,
        Inherited = false)]
    internal sealed class SupportedOSPlatformAttribute : Attribute
    {
        public SupportedOSPlatformAttribute(string platformName) =>
            this.PlatformName = platformName;

        public string PlatformName { get; }
    }
}
#endif
