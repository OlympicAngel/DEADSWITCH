using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Deadswitch.Game.Editor
{
    /// <summary>Imports the balance file (.toml) as a TextAsset so the runtime can load it from Resources (ADR-0008).</summary>
    [ScriptedImporter(1, "toml")]
    public sealed class TomlImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var text = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("text", text);
            ctx.SetMainObject(text);
        }
    }
}
