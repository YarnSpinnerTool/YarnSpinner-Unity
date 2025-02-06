using System.Collections.Generic;
using System.Text;
using Yarn.Markup;
using Yarn.Unity;
using UnityEngine;

[System.Serializable]
public struct EntityMap
{
    public string name;
    public UnityEngine.Color colour;
}

// also make a basic replacer that automatically replaces [i] and [b] with <i> and <b>
public class EntityColourer : Yarn.Unity.ReplacementMarkupHandler
{
    public EntityMap[] entities;
    public override List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(MarkupAttribute marker, StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode)
    {
        // this works in one of two ways
        // if we have a name string property we use that
        // otherwise read the contents of the text within the marker

        string nameText = string.Empty;
        if (marker.TryGetProperty("name", out var value))
        {
            if (value.Type != MarkupValueType.String)
            {
                var diagnostic = new LineParser.MarkupDiagnostic("We have a name property but it is not a string.");
                return new List<LineParser.MarkupDiagnostic>() {diagnostic};
            }
            nameText = value.StringValue.ToLower();
        }

        if (nameText == string.Empty)
        {
            nameText = childBuilder.ToString().ToLower();
        }

        foreach (var entity in entities)
        {
            if (entity.name.ToLower() == nameText)
            {
                childBuilder.Insert(0, $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGBA(entity.colour)}><b>");
                childBuilder.Append("</b></color>");
            }
        }

        return ReplacementMarkupHandler.NoDiagnostics;
    }
    
    protected void Start()
    {
        var lineProvider = (LineProviderBehaviour)GameObject.FindAnyObjectByType<DialogueRunner>().LineProvider;
        lineProvider.RegisterMarkerProcessor("name", this);
    }
}
