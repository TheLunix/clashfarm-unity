
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scenes : MonoBehaviour 
{
	public void NextLevel(int _sceneName)
	{
		SceneManager.LoadScene(_sceneName);
	}
}
