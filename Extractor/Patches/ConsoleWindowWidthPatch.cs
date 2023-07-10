using HarmonyLib;
namespace Extractor.Patches;

/// <summary>
/// Patch for the Console class to simulate window width.
/// This patch is necessary when invoking Media Tools from python,
/// as the console doesn't get initialized in that scenario.
/// This way, we can effectively use TreeBasedCli.
/// </summary>
[HarmonyPatch(typeof(Console))]
[HarmonyPatchCategory(PatchCategories.ConsoleHandlePatches)]
public static class ConsoleWindowWidthPatch
{
    // Default and minimum window width
    private const int DefaultWindowWidth = 120;
    private const int MinWindowWidth = 40;
    private const int MaxWindowWidth = 240;

    // This field is used to store a fake window width value
    private static int _fakeWindowWidth = DefaultWindowWidth;


    // ReSharper disable once InconsistentNaming
    // ReSharper disable once RedundantAssignment

    [HarmonyPrefix]
    [HarmonyPatch("WindowWidth", MethodType.Getter)]
    private static bool WindowWidthGetter(ref int __result)
    {
        __result = _fakeWindowWidth;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("WindowWidth", MethodType.Setter)]
    private static bool WindowWidthSetter(int value)
    {
        _fakeWindowWidth = value switch
        {
            // Only set the window width if it's within a reasonable range
            > MaxWindowWidth => MaxWindowWidth,
            < MinWindowWidth => MinWindowWidth,
            _ => value
        };

        return false;
    }
}
