using UnityEngine;
using TMPro; // 引入 TextMeshPro 命名空间

public class Marble : MonoBehaviour
{
    public TMP_Text characterText; // 拖拽你的文字子对象到这里
    public int indexInChain;       // 在珠子链中的索引
    public float positionOnPath;   // 在路径上的位置 (0.0 to 1.0)

    private char _character;

    // 设置珠子显示的文字
    public void SetCharacter(char c)
    {
        _character = c;
        characterText.text = c.ToString();
    }

    public char GetCharacter()
    {
        return _character;
    }
}