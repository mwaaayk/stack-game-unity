using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _score;
    [SerializeField] private TextMeshProUGUI _gameOverScore;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _startPanel;

    public void UpdateScoreDisplay(int score) => _score.text = score.ToString();

    public void ShowGameOver()
    {
        _gameOverScore.text = _score.text;
        _gameOverPanel.SetActive(true);
        _score.enabled = false;
    }

    public void HideStartPanel()
    {
        _score.enabled = true;
        _score.text = "0";
        _startPanel.SetActive(false);
    }

    private void ShowStartPanel() => _startPanel.SetActive(true);

    public void Retry()
    {
        _gameOverPanel.SetActive(false);
        ShowStartPanel();
    }
}
