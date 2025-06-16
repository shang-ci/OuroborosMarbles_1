using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneJumpButton : MonoBehaviour
{
    // 在Inspector中填写要跳转的场景名
    public string targetSceneName;

    // 按钮点击时调用此方法
    public void JumpToScene()
    {
        SceneManager.LoadScene(targetSceneName);
        
    }
}