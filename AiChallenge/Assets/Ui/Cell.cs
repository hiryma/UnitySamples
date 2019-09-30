using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logic;

namespace Ui
{
    public class Cell : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler
    {
        [SerializeField] Sprite lionSprite;
        [SerializeField] Sprite giraffeSprite;
        [SerializeField] Sprite elephantSprite;
        [SerializeField] Sprite chickSprite;
        [SerializeField] Sprite roosterSprite;
        [SerializeField] Image pieceImage;
        [SerializeField] GameObject hpRoot;
        [SerializeField] Text hpText;
        [SerializeField] Transform hpGaugeTransform;
        public bool Clicked { get; private set; }
        public bool Dragged { get; private set; }
        public bool Dropped { get; private set; }

        public void ManualUpdate(Piece logicPiece)
        {
            Clicked = Dragged = Dropped = false;
            Sprite sprite = null;
            if (logicPiece != null)
            {
                switch (logicPiece.type)
                {
                    case Piece.Type.Lion: sprite = lionSprite; break;
                    case Piece.Type.Giraffe: sprite = giraffeSprite; break;
                    case Piece.Type.Elephant: sprite = elephantSprite; break;
                    case Piece.Type.Chick:
                        if (logicPiece.flipped)
                        {
                            sprite = roosterSprite;
                        }
                        else
                        {
                            sprite = chickSprite;
                        }
                        break;
                }
                if (logicPiece.playerIndex == 0)
                {
                    pieceImage.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                }
                else
                {
                    pieceImage.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
                hpText.text = logicPiece.hp.ToString("F0");
                hpGaugeTransform.localScale = new Vector3(logicPiece.hp / logicPiece.maxHp, 1f, 1f);
            }
            if (sprite == null)
            {
                pieceImage.enabled = false;
                hpRoot.SetActive(false);
            }
            else
            {
                pieceImage.sprite = sprite;
                pieceImage.enabled = true;
                hpRoot.SetActive(true);
            }
        }

        public void OnPointerClick(PointerEventData data)
        {
            Clicked = true;
        }

        public void OnBeginDrag(PointerEventData data)
        {
            // 何もしないが、ないとEndDrag来ない?
        }

        public void OnDrag(PointerEventData data)
        {
            // 何もしないが、ないとEndDrag来ない
        }

        public void OnEndDrag(PointerEventData data)
        {
            Dragged = true;
        }

        public void OnDrop(PointerEventData data)
        {
            Dropped = true;
        }
    }

}