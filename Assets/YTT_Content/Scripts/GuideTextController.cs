using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GuideTextController : MonoBehaviour
{
    public TextMeshProUGUI guideText;
    public List<string> storySegments;
    private int currentIndex = 0;
    public string NextScene;
    public float typeSpeed; // 每个字出现的间隔时间

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    public Image guideImage; // 在Inspector拖入你的Image组件
    public List<Sprite> storyImages; // 每段文字对应一张图片

    void Start()
    {
        StartTyping(storySegments[currentIndex]);
        if (storyImages != null && storyImages.Count > 0 && guideImage != null)
        {
            guideImage.sprite = storyImages[0];
        }
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
                    // 切换图片
                    if (storyImages != null && currentIndex < storyImages.Count && guideImage != null)
                    {
                        guideImage.sprite = storyImages[currentIndex];
                    }
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