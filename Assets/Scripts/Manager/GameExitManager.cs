using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;


public class GameExitManager : MonoBehaviourPunCallbacks
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
        Debug.Log("방 나가기 요청...");
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            MoveToLobby();
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("방 나가기 완료 콜백 수신 로비로 이동합니다");
        MoveToLobby();
    }
    private void MoveToLobby()
    {
        _isExiting = false;

        // 메시지 큐 정지 (동기화 에러 방지용)
        PhotonNetwork.IsMessageQueueRunning = false;

        SceneManager.LoadScene("Loby");

        // 씬 로드 후 다시 켜줌
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
