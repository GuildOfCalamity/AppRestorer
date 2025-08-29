using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

// Wrap non-Exception throws in a System.Runtime.CompilerServices.RuntimeWrappedException.
// Ensures that throwing a non-Exception type (e.g., an integer) still results in a catchable exception object.
// Improves language interoperability and safer error handling.
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]

// Lets you attach arbitrary key/value pairs to your assembly’s metadata.
// Can be read at runtime via reflection GetCustomAttributes<AssemblyMetadataAttribute>().
// Handy for linking CI build numbers, source branches, or proprietary tags.
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/GuildOfCalamity/AppRestorer")]
[assembly: AssemblyMetadata("RepositoryType", "git")]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]
