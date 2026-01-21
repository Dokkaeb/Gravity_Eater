using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Threading.Tasks;


public class GameExitManager : MonoBehaviour
{
    public static GameExitManager Instance { get; private set; }

    bool _isExiting = false;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //메인나갈때 호출할 비동기 메서드
    public async void ExitToMain(float currentScore)
    {

        if (_isExiting) return;
        _isExiting = true;

        PhotonNetwork.AutomaticallySyncScene = false;

        Debug.Log("게임종료중 점수 저장함");
        if(FirebaseManager.Instance != null)
        {
            string nick = PhotonNetwork.LocalPlayer.NickName;
            if (string.IsNullOrEmpty(nick)) nick = "Guest";

            await FirebaseManager.Instance.UpdateHighScore(nick,currentScore); //저장끝날때까지 대기
        }

        //방나가기
        Debug.Log("2. 포톤 방 나가기 요청...");
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();

            //방 나갈때까지 대기, 3초정도만
            float timeout = 3f;
            while (PhotonNetwork.InRoom && timeout > 0)
            {
                timeout -= Time.deltaTime;
                await Task.Yield(); // 한 프레임 대기
            }
        }

        Debug.Log("3. 메인 씬으로 이동...");
        _isExiting = false;
        PhotonNetwork.IsMessageQueueRunning = false;
        SceneManager.LoadScene("Loby");
        PhotonNetwork.IsMessageQueueRunning = true;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
