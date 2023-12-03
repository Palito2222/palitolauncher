using UnityEngine;

public class ChangeGameUI : MonoBehaviour
{
    [SerializeField] private GameObject game1GO;
    [SerializeField] private GameObject game2GO;
    [SerializeField] private GameObject game3GO;

    public void ChangeGame1UI()
    {
        if (game1GO.activeSelf)
        {
            return;
        }

        game2GO.SetActive(false);
        game3GO.SetActive(false);
        game1GO.SetActive(true);
    }

    public void ChangeGame2UI()
    {
        if (game2GO.activeSelf)
        {
            return;
        }

        game1GO.SetActive(false);
        game3GO.SetActive(false);
        game2GO.SetActive(true);
    }

    public void ChangeGame3UI()
    {
        if (game3GO.activeSelf)
        {
            return;
        }

        game1GO.SetActive(false);
        game2GO.SetActive(false);
        game3GO.SetActive(true);
    }
}
