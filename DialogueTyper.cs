using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueTyper : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;
    public string[] lines;
    public float typingSpeed = 0.03f;

    private int currentLineIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    public Button nextButton;

    void Start()
    {
        ShowNextLine();
        nextButton.onClick.AddListener(OnNextClicked);
    }

    void OnNextClicked()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = lines[currentLineIndex];
            isTyping = false;
        }
        else
        {
            currentLineIndex++;
            if (currentLineIndex < lines.Length)
            {
                ShowNextLine();
            }
            else
            {
                Debug.Log("대화 끝!");
                // 씬 전환이나 다음 연출 호출 가능
            }
        }
    }

    void ShowNextLine()
    {
        typingCoroutine = StartCoroutine(TypeLine(lines[currentLineIndex]));
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }
}