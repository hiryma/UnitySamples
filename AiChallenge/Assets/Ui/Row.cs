using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ui
{

    public class Row : MonoBehaviour
    {
        [SerializeField] Cell[] cells;
        public int Length { get { return cells.Length;  } }

        public Cell this[int i]
        {
            get
            {
                return cells[i];
            }
        }
    }
}