using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity {

    [HelpURL("https://yarnspinner.dev/docs/unity/components/yarn-programs/")]
    public class YarnProgram : ScriptableObject {

        [SerializeField]
        [HideInInspector]
        public byte[] compiledYarnProgram;

        public TextAsset defaultStringTable;

        /// <summary>
        /// Deserializes a compiled Yarn program from the stored bytes in this
        /// object.
        /// </summary>
        public Program GetProgram()
        {
            return Program.Parser.ParseFrom(compiledYarnProgram);
        }
    }

}
