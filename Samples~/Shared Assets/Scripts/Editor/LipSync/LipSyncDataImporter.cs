using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;

#nullable enable

namespace Yarn.Unity.Samples.Editor
{

    /// <summary>
    /// A custom importer that takes TSV files containing lipsync data, and
    /// produces <see cref="LipSyncedVoiceLine"/> assets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// .lipsync files are expected to contain tab-delimited data in two
    /// columns: the time that the mouth shape should start appearing, and the
    /// name of the mouth shape (see <see cref="LipSyncedVoiceLine.MouthShape"/>
    /// for valid values).
    /// </para>
    /// <para>As an exception, if a line begins with "audio:" followed by a
    /// GUID, the <see cref="AudioClip"/> referred to by that GUID will be used
    /// for the <see cref="LipSyncedVoiceLine.audioClip"/> reference.</para>
    /// </remarks>

    // Audio clips have an importer priority of 1100, and scripted importers
    // have a default importer priority of 1000 (source:
    // https://discussions.unity.com/t/understanding-import-order-of-native-unity-asset-types/859814/4).
    // So, we'll offset by 150 to get 1000+150=1150, which is after audio clips.
    // This means that we'll import after audio, which means that we can
    // correctly get AudioClip references that may have imported alongside this
    // asset.
    [ScriptedImporter(1, "lipsync", importQueueOffset: 150)]
    public class LipSyncDataImporter : ScriptedImporter
    {
        public AudioClip? audioClip;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var data = ScriptableObject.CreateInstance<LipSyncedVoiceLine>();

            var lines = File.ReadAllLines(ctx.assetPath);

            ctx.AddObjectToAsset("data", data);
            ctx.SetMainObject(data);

            if (audioClip != null)
            {
                data.audioClip = audioClip;
            }

            foreach (var line in lines)
            {
                try
                {
                    if (line.StartsWith("audio:"))
                    {
                        var guid = line["audio:".Length..].Trim();
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        AudioClip clip;

                        if (path == null)
                        {
                            Debug.LogError($"{nameof(LipSyncedVoiceLine)} {name} can't find an asset with GUID {guid}", this);
                            continue;
                        }
                        else if ((clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path)) == null)
                        {
                            Debug.LogError($"{nameof(LipSyncedVoiceLine)} {name} can't import audio: asset at {path} is not an {nameof(AudioClip)}", this);
                            continue;
                        }
                        else
                        {
                            data.audioClip = clip;
                            continue;
                        }
                    }

                    var lineData = line.Split('\t');

                    if (lineData.Length < 2)
                    {
                        continue;
                    }

                    float time = float.Parse(lineData[0]);
                    LipSyncedVoiceLine.MouthShape mouthShape = (LipSyncedVoiceLine.MouthShape)System.Enum.Parse(typeof(LipSyncedVoiceLine.MouthShape), lineData[1]);

                    LipSyncedVoiceLine.MouthShapeFrame frame = new()
                    {
                        time = time,
                        mouthShape = mouthShape
                    };

                    if (lineData.Length >= 3)
                    {
                        frame.comment = lineData[2];
                    }

                    data.frames.Add(frame);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    return;
                }
            }
        }
    }
}