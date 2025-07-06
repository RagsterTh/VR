using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using Photon.Pun;

public class Balcony : MonoBehaviour
{
    [SerializeField] private bool _isInConstruction;
    [SerializeField] private Canvas _dialogueCanvas;
    [SerializeField] private TextMeshProUGUI _dialogueText;
    [SerializeField] private TextMeshProUGUI _merchantNameTMP;
    [SerializeField] private string _merchantName;
    [SerializeField] private List<string> _dialogueLines;
    [SerializeField] private float _textSpeed = 0.05f;
    [SerializeField] private Karen _karen;

    private int _currentLineIndex = 0;
    private Coroutine _typingCoroutine;
    private bool _isTyping = false;
    private bool _playerInRange = false;
    PhotonView _phView;
    public UnityEvent OnDialogueEnd;
    private void Start()
    {
        _phView = GetComponent<PhotonView>();
        _merchantNameTMP.text = _merchantName;
        _dialogueCanvas.gameObject.SetActive(false);


        //Tempor�rio
        _playerInRange = true;
        _dialogueCanvas.gameObject.SetActive(true);
        StartDialogue();
        if (!_isInConstruction)
            Debug.Log("Triggering animation (placeholder)");
    }

    private void OnTriggerEnter(Collider other)
    {
        /*
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
            _dialogueCanvas.gameObject.SetActive(true);
            StartDialogue();
            if(!_isInConstruction)
                Debug.Log("Triggering animation (placeholder)");
        }
        */
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            EndDialogue();
        }
    }

    private void StartDialogue()
    {
        _currentLineIndex = 0;
        DisplayCurrentLine();
    }

    private void EndDialogue()
    {
        _dialogueCanvas.gameObject.SetActive(false);
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _dialogueText.text = "";
    }
    [PunRPC]
    void RPC_ExitLobby()
    {
        OnDialogueEnd.Invoke();
    }
    private void DisplayCurrentLine()
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _typingCoroutine = StartCoroutine(TypeLine(_dialogueLines[_currentLineIndex]));
        _karen.ChangeSound(_currentLineIndex);
    }

    private IEnumerator TypeLine(string line)
    {
        _isTyping = true;
        _dialogueText.text = "";
        foreach (char c in line)
        {
            _dialogueText.text += c;
            yield return new WaitForSeconds(_textSpeed);
        }
        _isTyping = false;
        //Tempor�rio
        yield return new WaitForSeconds(4);
        AdvanceDialogue();
    }

    public void AdvanceDialogue()
    {
        if (!_playerInRange || _dialogueLines.Count == 0) return;

        if (_isTyping)
        {
            StopCoroutine(_typingCoroutine);
            _dialogueText.text = _dialogueLines[_currentLineIndex];
            _isTyping = false;
        }
        else
        {
            _currentLineIndex++;
            if (_currentLineIndex < _dialogueLines.Count)
            {
                DisplayCurrentLine();
            }
            else
            {
                EndDialogue();
                if(PhotonNetwork.IsMasterClient)
                    _phView.RPC("RPC_ExitLobby", RpcTarget.AllBuffered);
            }
        }
    }
}
