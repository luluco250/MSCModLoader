﻿//Heavily modified Unity UI extensions (old ass version) (BSD3 license)
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace MSCLoader
{

    [RequireComponent(typeof(RectTransform))]
    internal class DropDownList : MonoBehaviour
    {
        public Color disabledTextColor;
        public DropDownListItem SelectedItem { get; private set; } //outside world gets to get this, not set it

        public List<DropDownListItem> Items;
        public System.Action<int> OnSelectionChanged; // fires when selection is changed;

        //private bool isInitialized = false;
        private bool _isPanelActive = false;
        private bool _hasDrawnOnce = false;

        private DropDownListButton _mainButton;

        private RectTransform _rectTransform;

        public RectTransform _overlayRT;
        private RectTransform _scrollPanelRT;
        private RectTransform _scrollBarRT;
        private RectTransform _slidingAreaRT;
        //   private RectTransform scrollHandleRT;
        private RectTransform _itemsPanelRT;
    //	private Canvas _canvas;
        //private RectTransform _canvasRT;

        //private ScrollRect _scrollRect;

        private List<DropDownListButton> _panelItems;

        private GameObject _itemTemplate;

        [SerializeField]
        private float _scrollBarWidth = 4.0f;
        public float ScrollBarWidth
        {
            get { return _scrollBarWidth; }
            set
            {
                _scrollBarWidth = value;
                RedrawPanel();
            }
        }
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                StartCoroutine(DelayedUpdate());
            }
        }
        //    private int scrollOffset; //offset of the selected item
        private int _selectedIndex = 0;


        [SerializeField]
        private int _itemsToDisplay;
        public int ItemsToDisplay
        {
            get { return _itemsToDisplay; }
            set
            {
                _itemsToDisplay = value;
                RedrawPanel();
            }
        }

        public void Start()
        {
            Initialize();
        }

        IEnumerator DelayedUpdate()
        {
            yield return null;
            UpdateSelected(); 
        }
        private bool Initialize()
        {
            bool success = true;
            try
            {
                _rectTransform = GetComponent<RectTransform>();
                _mainButton = new DropDownListButton(_rectTransform.FindChild("MainButton").gameObject);

                //_overlayRT = _rectTransform.FindChild("Overlay").GetComponent<RectTransform>();
                _overlayRT.gameObject.SetActive(false);


                _scrollPanelRT = _overlayRT.FindChild("ScrollPanel").GetComponent<RectTransform>();
                _scrollBarRT = _scrollPanelRT.FindChild("Scrollbar").GetComponent<RectTransform>();
                _slidingAreaRT = _scrollBarRT.FindChild("SlidingArea").GetComponent<RectTransform>();
                //  scrollHandleRT = slidingAreaRT.FindChild("Handle").GetComponent<RectTransform>();
                _itemsPanelRT = _scrollPanelRT.FindChild("Items").GetComponent<RectTransform>();
                //itemPanelLayout = itemsPanelRT.gameObject.GetComponent<LayoutGroup>();

                //_canvas = GetComponentInParent<Canvas>();
                //_canvasRT = _canvas.GetComponent<RectTransform>();

                //_scrollRect = _scrollPanelRT.GetComponent<ScrollRect>();
                //_scrollRect.scrollSensitivity = _rectTransform.sizeDelta.y / 2;
                //_scrollRect.movementType = ScrollRect.MovementType.Clamped;
                //_scrollRect.content = _itemsPanelRT;


                _itemTemplate = _rectTransform.FindChild("ItemTemplate").gameObject;
                _itemTemplate.SetActive(false);
            }
            catch (System.NullReferenceException ex)
            {
                Debug.LogException(ex);
                Debug.LogError("Something is setup incorrectly with the dropdownlist component causing a Null Refernece Exception");
                success = false;
            }

            _panelItems = new List<DropDownListButton>();

            RebuildPanel();
            RedrawPanel();
            UpdateSelected();
            return success;
        }

        /* currently just using items in the list instead of being able to add to it.
        public void AddItems(params object[] list)
        {
            List<DropDownListItem> ddItems = new List<DropDownListItem>();
            foreach (var obj in list)
            {
                if (obj is DropDownListItem)
                {
                    ddItems.Add((DropDownListItem)obj);
                }
                else if (obj is string)
                {
                    ddItems.Add(new DropDownListItem(caption: (string)obj));
                }
                else if (obj is Sprite)
                {
                    ddItems.Add(new DropDownListItem(image: (Sprite)obj));
                }
                else
                {
                    throw new System.Exception("Only ComboBoxItems, Strings, and Sprite types are allowed");
                }
            }
            Items.AddRange(ddItems);
            Items = Items.Distinct().ToList();//remove any duplicates
            RebuildPanel();
        }
        */

        /// <summary>
        /// Rebuilds the contents of the panel in response to items being added.
        /// </summary>
        private void RebuildPanel()
        {
            if (Items.Count == 0) return;

            int indx = _panelItems.Count;
            while (_panelItems.Count < Items.Count)
            {
                GameObject newItem = Instantiate(_itemTemplate) as GameObject;
                newItem.name = "Item " + indx;
                newItem.transform.SetParent(_itemsPanelRT, false);

                _panelItems.Add(new DropDownListButton(newItem));
                indx++;
            }
            for (int i = 0; i < _panelItems.Count; i++)
            {
                if (i < Items.Count)
                {
                    DropDownListItem item = Items[i];

                    _panelItems[i].txt.text = item.Caption.ToUpper();
                    if (item.IsDisabled) _panelItems[i].txt.color = disabledTextColor;

                    if (_panelItems[i].btnImg != null) _panelItems[i].btnImg.sprite = null;//hide the button image  
                    _panelItems[i].img.sprite = item.Image;
                    _panelItems[i].img.color = (item.Image == null) ? new Color(1, 1, 1, 0)
                                                                    : item.IsDisabled ? new Color(1, 1, 1, .5f)
                                                                                      : Color.white;
                    int ii = i; //have to copy the variable for use in anonymous function
                    _panelItems[i].btn.onClick.RemoveAllListeners();
                    _panelItems[i].btn.onClick.AddListener(() =>
                    {
                        OnItemClicked(ii);
                        if (item.OnSelect != null) item.OnSelect();
                    });
                }
                _panelItems[i].gameobject.SetActive(i < Items.Count);// if we have more thanks in the panel than Items in the list hide them
            }
        }

        private void OnItemClicked(int indx)
        {
            //	Debug.Log("item " + indx + " clicked");
            if (indx != _selectedIndex && OnSelectionChanged != null)
            {
                _selectedIndex = indx;
                UpdateSelected();
                OnSelectionChanged(indx);
            }

            ToggleDropdownPanel(true);
        }

        private void UpdateSelected()
        {
            SelectedItem = (_selectedIndex > -1 && _selectedIndex < Items.Count) ? Items[_selectedIndex] : null;
            if (SelectedItem == null)
            {
#if !Mini
                ModConsole.Error("[DropDownList] SelectedIndex value out of bounds.");
#endif
                return;
            }

            bool hasImage = SelectedItem.Image != null;
            if (hasImage)
            {
                _mainButton.img.sprite = SelectedItem.Image;
                _mainButton.img.color = Color.white;

                //if (Interactable) mainButton.img.color = Color.white;
                //else mainButton.img.color = new Color(1, 1, 1, .5f);
            }
            else
            {
                _mainButton.img.sprite = null;
            }

            _mainButton.txt.text = SelectedItem.Caption.ToUpper();

            //update selected index color
            for (int i = 0; i < _itemsPanelRT.childCount; i++)
            {
                if (_selectedIndex == i) {
                //	_panelItems[i].btnImg.color = _mainButton.btn.colors.highlightedColor;
                    _panelItems[i].btn.transition = Selectable.Transition.None;


                }
                else
                {
                //	_panelItems[i].btnImg.color = new Color(0, 0, 0, 255);
                    _panelItems[i].btn.transition = Selectable.Transition.ColorTint;

                }
            }
        }


        private void RedrawPanel()
        {
            float scrollbarWidth = Items.Count > ItemsToDisplay ? _scrollBarWidth : 0f;

            if (!_hasDrawnOnce || _rectTransform.sizeDelta != _mainButton.rectTransform.sizeDelta)
            {
                _hasDrawnOnce = true;
                //_mainButton.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _rectTransform.sizeDelta.x);
            //	_mainButton.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _rectTransform.sizeDelta.y);
                //_mainButton.txt.rectTransform.offsetMax = new Vector2(4, 0);

                //_scrollPanelRT.SetParent(transform, true);//break the scroll panel from the overlay
                //_scrollPanelRT.anchoredPosition = new Vector2(0, -_rectTransform.sizeDelta.y); //anchor it to the bottom of the button

                //make the overlay fill the screen
                //_overlayRT.SetParent(_canvas.transform, false); //attach it to top level object
                //_overlayRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _canvasRT.sizeDelta.x);
                //_overlayRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _canvasRT.sizeDelta.y);

                //_overlayRT.SetParent(transform.parent, true);//reattach to this object
                //_scrollPanelRT.SetParent(_overlayRT, true); //reattach the scrollpanel to the overlay            
            }

            if (Items.Count < 1) return;

            float dropdownHeight = (25 * Mathf.Min(_itemsToDisplay, Items.Count))+5;
            _scrollPanelRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dropdownHeight);
            //_scrollPanelRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _rectTransform.sizeDelta.x);
            _overlayRT.GetComponent<LayoutElement>().preferredHeight = dropdownHeight;
            _itemsPanelRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _scrollPanelRT.sizeDelta.x - scrollbarWidth - 5);
            _itemsPanelRT.anchoredPosition = new Vector2(5, 0);

            _scrollBarRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scrollbarWidth);
            _scrollBarRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dropdownHeight);

            _slidingAreaRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
            _slidingAreaRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dropdownHeight - _scrollBarRT.sizeDelta.x);
        }

        /// <summary>
        /// Toggle the drop down list
        /// </summary>
        /// <param name="directClick"> whether an item was directly clicked on</param>
        public void ToggleDropdownPanel(bool directClick)
        {
            //_overlayRT.transform.localScale = new Vector3(1, 1, 1);
            //_scrollBarRT.transform.localScale = new Vector3(1, 1, 1);
            _isPanelActive = !_isPanelActive;
            _overlayRT.gameObject.SetActive(_isPanelActive);
            /*if (_isPanelActive)
            {
                transform.SetAsLastSibling();
            }
            else if (directClick)
            {
                // scrollOffset = Mathf.RoundToInt(itemsPanelRT.anchoredPosition.y / _rectTransform.sizeDelta.y); 
            }*/
        }
    }
}
