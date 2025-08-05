using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopSelecSection : MonoBehaviour
{
    [SerializeField] public Image PrevievButton, NowButton;
	[SerializeField] public GameObject PrevievItemPanel, NowItemPanel;
	[SerializeField] private Sprite[] ShopButtonActive, ShopButtonNotActive;
	[SerializeField] public int PButton;
	private Image ImageButton;
	private int NButton;
	public void ChangeActiveButton(Image ClicImage)
	{
		NowButton = ClicImage;
		NowButton.sprite = ShopButtonActive[NButton];
		PrevievButton.sprite = ShopButtonNotActive[PButton];
		PButton = NButton;
		PrevievButton = NowButton;
		NowButton = null;
	}		
	public void ChangeActiveItemPanel(GameObject ItemPanel)
	{
		NowItemPanel = ItemPanel;
		NowItemPanel.SetActive(true);
		PrevievItemPanel.SetActive(false);
		PrevievItemPanel = NowItemPanel;
		NowItemPanel = null;
	}
	public void SetNumberIcon(int id)
	{
		NButton = id;
	}	

}
