using System.Collections.Generic;

#if USE_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Yarn.Unity
{
    public class TextLineProvider : LineProviderBehaviour
    {
        /// <summary>Specifies the language code to use for text content
        /// for this <see cref="TextLineProvider"/>.
        [Language]
        public string textLanguageCode = System.Globalization.CultureInfo.CurrentCulture.Name;

        public override LocalizedLine GetLocalizedLine(Yarn.Line line)
        {
            var text = YarnProject.GetLocalization(textLanguageCode).GetLocalizedString(line.ID);
            return new LocalizedLine()
            {
                TextID = line.ID,
                RawText = text,
                Substitutions = line.Substitutions,
                Metadata = YarnProject.lineMetadata.GetMetadata(line.ID),
            };
        }

        public override void PrepareForLines(IEnumerable<string> lineIDs)
        {
            // No-op; text lines are always available
        }

        public override bool LinesAvailable => true;

        public override string LocaleCode => textLanguageCode;
    }
}
