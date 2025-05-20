/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Yarn.Unity.Editor
{
    /// <summary>
    /// Contains utility methods for working with Yarn Spinner content in
    /// the Unity Editor.
    /// </summary>
    public static class YarnEditorUtility
    {

        // GUID for editor assets. (Doing it like this means that we don't
        // have to worry about where the assets are on disk, if the user
        // has moved Yarn Spinner around.)
        const string DocumentIconTextureGUID = "0ed312066ea6f40f6af965f21c818b34";
        const string ProjectIconTextureGUID = "f6a533d9225cd40ea9ded31d4f686e3b";
        const string LocalizationIconTextureGUID = "2cbba4ddd142149b0a38697070990deb";
        const string YarnScriptTemplateFileGUID = "4f4ca4a46020a454f80e2ac78eda5aa1";
        const string DialoguePresenterTemplateFileGUID = "4a168359cda6140c0bddcd5955a326e4";

        /// <summary>
        /// Returns a <see cref="Texture2D"/> that can be used to represent
        /// Yarn files.
        /// </summary>
        /// <returns>A texture to use in the Unity editor for Yarn
        /// files.</returns>
        public static Texture2D GetYarnDocumentIconTexture()
        {
            string textureAssetPath = AssetDatabase.GUIDToAssetPath(DocumentIconTextureGUID);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
        }

        /// <summary>
        /// Returns a <see cref="Texture2D"/> that can be used to represent
        /// Yarn project files.
        /// </summary>
        /// <returns>A texture to use in the Unity editor for Yarn project
        /// files.</returns>
        public static Texture2D GetYarnProjectIconTexture()
        {
            string textureAssetPath = AssetDatabase.GUIDToAssetPath(ProjectIconTextureGUID);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
        }

        /// <summary>
        /// Returns a <see cref="Texture2D"/> that can be used to represent
        /// built-in Localization objects.
        /// </summary>
        /// <returns>A texture to use in the Unity editor for Yarn built-in
        /// Localization files.</returns>
        public static Texture2D GetLocalizationIconTexture()
        {
            string textureAssetPath = AssetDatabase.GUIDToAssetPath(LocalizationIconTextureGUID);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
        }

        /// <summary>
        /// Returns the path to a text file that can be used as the basis
        /// for newly created Yarn scripts.
        /// </summary>
        /// <returns>A path to a file to use in the Unity editor for
        /// creating new Yarn scripts.</returns>
        /// <throws cref="FileNotFoundException">Thrown if the template
        /// text file cannot be found.</throws>
        public static string GetTemplateYarnScriptPath()
        {
            var path = AssetDatabase.GUIDToAssetPath(YarnScriptTemplateFileGUID);
            if (string.IsNullOrEmpty(path))
            {
                throw new System.IO.FileNotFoundException($"Template file for new Yarn scripts couldn't be found. Have the .meta files for Yarn Spinner been modified or deleted? Try re-importing the Yarn Spinner package to fix this error.");
            }
            return path;
        }

        /// <summary>
        /// Returns the path to a text file that can be used as the basis
        /// for newly created C# Dialogue Presenter scripts.
        /// </summary>
        /// <returns>A path to a file to use in the Unity editor for
        /// creating new C# Dialogue Presenter.</returns>
        /// <throws cref="FileNotFoundException">Thrown if the template
        /// text file cannot be found.</throws>
        public static string GetTemplateDialoguePresenterPath()
        {
            var path = AssetDatabase.GUIDToAssetPath(DialoguePresenterTemplateFileGUID);
            if (string.IsNullOrEmpty(path))
            {
                throw new System.IO.FileNotFoundException($"Template file for Dialogue View scripts couldn't be found. Have the .meta files for Yarn Spinner been modified or deleted? Try re-importing the Yarn Spinner package to fix this error.");
            }
            return path;
        }


        /// <summary>
        /// Begins the interactive process of creating a new Yarn file in
        /// the Editor.
        /// </summary>    
        [MenuItem("Assets/Create/Yarn Spinner/Yarn Script", false, 10)]
        public static void CreateYarnAsset()
        {

            // This method call is undocumented, but public. It's defined
            // in ProjectWindowUtil, and used by other parts of the editor
            // to create other kinds of assets (scripts, textures, etc).
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreateYarnScriptAsset>(),
                "NewYarnScript.yarn",
                GetYarnDocumentIconTexture(),
                GetTemplateYarnScriptPath());
        }

        /// <summary>
        /// Creates a new Yarn Project asset in the current folder, and begins
        /// interactively renaming it.
        /// </summary>
        [MenuItem("Assets/Create/Yarn Spinner/Yarn Project", false, 101)]
        public static void CreateYarnProject()
        {
            // This method call is undocumented, but public. It's defined
            // in ProjectWindowUtil, and used by other parts of the editor
            // to create other kinds of assets (scripts, textures, etc).
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreateYarnProjectAsset>(),
                "NewProject.yarnproject",
                GetYarnProjectIconTexture(),
                GetTemplateYarnScriptPath());
        }

        /// <summary>
        /// Creates a new C# script asset containing a template Dialogue Presenter in
        /// the current folder, and begins interactively renaming it.
        /// </summary>
        [MenuItem("Assets/Create/Yarn Spinner/Dialogue Presenter Script", false, 111)]
        [MenuItem("Assets/Create/Scripting/Yarn Spinner/Dialogue Presenter Script", false, 101)]
        public static void CreateDialoguePresenterScript()
        {
            // This method call is undocumented, but public. It's defined
            // in ProjectWindowUtil, and used by other parts of the editor
            // to create other kinds of assets (scripts, textures, etc).
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreateYarnScriptAsset>(),
                "NewDialoguePresenter.cs",
                null,
                GetTemplateDialoguePresenterPath());
        }

        /// <summary>
        /// Writes a Yarn Project to <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path at which to write the file.</param>
        /// <param name="project">The Yarn Project to write to disk.</param>
        public static Object CreateYarnProject(string path, Compiler.Project project)
        {
            var text = project.GetJson();
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Object>(path);
        }

        /// <summary>
        /// Creates a new Yarn script at the given path, using the default
        /// template.
        /// </summary>
        /// <param name="path">The path at which to create the
        /// script.</param>
        public static Object CreateYarnAsset(string path)
        {
            return CreateScriptAssetFromTemplate(path, GetTemplateYarnScriptPath());
        }

        private static Object CreateScriptAssetFromTemplate(string pathName, string resourceFile)
        {
            // Read the contents of the template file
            string templateContent;
            try
            {
                templateContent = File.ReadAllText(resourceFile);
            }
            catch
            {
                Debug.LogError("Failed to find template file. Creating an empty file instead.");
                templateContent = "";
            }

            // The file name is the name of the file, sans extension.
            string fileName = Path.GetFileNameWithoutExtension(pathName);

            // Replace any spaces with underscores - these aren't allowed
            // in node names
            fileName = fileName.Replace(" ", "_");

            // Replace the placeholder with the file name
            templateContent = templateContent.Replace("#SCRIPTNAME#", fileName);

            // Respect the user's line endings preferences for this new
            // text asset
            string unixLineEndings = "\n";
            string windowsLineEndings = "\r\n";
            string lineEndings;
            switch (EditorSettings.lineEndingsForNewScripts)
            {
                case LineEndingsMode.OSNative:
                    // OS native = use Windows if we're on Windows, else
                    // Unix
                    var isWindows = Application.platform == RuntimePlatform.WindowsEditor;
                    lineEndings = isWindows ? windowsLineEndings : unixLineEndings;
                    break;
                case LineEndingsMode.Windows:
                    // Windows = use Windows endings
                    lineEndings = windowsLineEndings;
                    break;
                case LineEndingsMode.Unix:
                default:
                    // Unix or anything else = use Unix endings
                    lineEndings = unixLineEndings;
                    break;
            }

            // Replace every line ending in the template (this way we don't
            // need to keep track of which line ending the asset was last
            // saved in)
            templateContent = System.Text.RegularExpressions.Regex.Replace(templateContent, @"\r\n?|\n", lineEndings);

            // Write it all out to disk as UTF-8
            var fullPath = Path.GetFullPath(pathName);
            File.WriteAllText(fullPath, templateContent, System.Text.Encoding.UTF8);

            // Force Unity to notice the new asset (this will also compile
            // the new, empty Yarn script)
            AssetDatabase.ImportAsset(pathName);

            // We don't hugely care about the details of the object anyway
            // (we just wanted to ensure that it's imported as at least an
            // asset), so we'll return it as an Object here.
            return AssetDatabase.LoadAssetAtPath<Object>(pathName);
        }

        // A handler that receives a callback after the user finishes
        // naming a new file.
        private class DoCreateYarnScriptAsset : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            // The user just finished typing (and didn't cancel it by
            // pressing escape or anything.) Commit the action by
            // generating the file on disk.
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                // Produce the asset.
                Object o = CreateScriptAssetFromTemplate(pathName, resourceFile);

                // Reveal it on disk.
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        // A handler that receives a callback after the user finishes
        // naming a new file.
        private class DoCreateYarnProjectAsset : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            // The user just finished typing (and didn't cancel it by
            // pressing escape or anything.) Commit the action by
            // generating the file on disk.
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                // Produce the asset.
                var project = YarnProjectUtility.CreateDefaultYarnProject();
                var json = project.GetJson();

                // Write it all out to disk as UTF-8
                var fullPath = Path.GetFullPath(pathName);
                File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);

                // Force Unity to notice the new asset.
                AssetDatabase.ImportAsset(pathName);

                Object o = AssetDatabase.LoadAssetAtPath<Object>(pathName);

                // Reveal it on disk.
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        /// <summary>
        /// Get all assets of a given type.
        /// </summary>
        /// <typeparam name="T">AssetImporter type to search for. Should be convertible from AssetImporter.</typeparam>
        /// <param name="filterQuery">Asset query (see <see cref="AssetDatabase.FindAssets(string)"/> documentation for formatting).</param>
        /// <param name="converter">Custom type caster.</param>
        /// <returns>Enumerable of all assets of a given type.</returns>
        public static IEnumerable<T> GetAllAssetsOf<T>(string filterQuery, System.Func<AssetImporter, T>? converter = null) where T : class
            => AssetDatabase.FindAssets(filterQuery)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetImporter.GetAtPath)
                .Select(importer => converter?.Invoke(importer) ?? importer as T)
                .Where(source => source != null)!;
    }
}
