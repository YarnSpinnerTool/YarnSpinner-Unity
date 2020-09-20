using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity {

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
