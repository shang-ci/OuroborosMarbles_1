using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GuideTextController : MonoBehaviour
{
    public TextMeshProUGUI guideText;
    public List<string> storySegments;  
    private int currentIndex = 0;
    public string NextScene;
    public float typeSpeed; // 每个字出现的间隔时间

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    void Start()
    {
        StartTyping(storySegments[currentIndex]);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                // 快速显示全部文字
                StopCoroutine(typingCoroutine);
                guideText.text = storySegments[currentIndex];
                isTyping = false;
            }
            else
            {
                currentIndex++;
                if (currentIndex < storySegments.Count)
                {
                    StartTyping(storySegments[currentIndex]);
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(NextScene);
                }
            }
        }
    }

    void StartTyping(string content)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(content));
    }

    IEnumerator TypeText(string content)
    {
        isTyping = true;
        guideText.text = "";
        foreach (char c in content)
        {
            guideText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
    }
}