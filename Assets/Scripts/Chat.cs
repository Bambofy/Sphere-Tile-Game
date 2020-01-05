using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chat : Bolt.EntityBehaviour<IChat>
{
    public GameObject ChatMessagePrefab;

    private GameObject _chatScrollView;
    private GameObject _chatScrollViewContent;

    private GameObject _chatWriteMessageButton;
    private GameObject _chatSendMessageButton;
    private GameObject _chatTextInputBox;
    private GameObject _chatExitWritingMessageButton;

    private List<GameObject> _chatMessageUIElements;

    private List<string> _messageLog = new List<string>();

    public override void Attached()
    {
        if (entity.IsOwner)
        {
            _chatMessageUIElements = new List<GameObject>();

            _chatScrollView = GameObject.FindGameObjectWithTag("CHAT_SCROLL_VIEW");
            _chatScrollViewContent = GameObject.FindGameObjectWithTag("CHAT_SCROLL_VIEW_CONTENT");

            _chatTextInputBox = GameObject.FindGameObjectWithTag("CHAT_INPUT_FIELD");
            _chatSendMessageButton = GameObject.FindGameObjectWithTag("CHAT_SUBMIT_BUTTON");
            _chatWriteMessageButton = GameObject.FindGameObjectWithTag("CHAT_WRITE_MESSAGE_BUTTON");
            _chatExitWritingMessageButton = GameObject.FindGameObjectWithTag("CHAT_STOP_WRITING_MESSAGE_BUTTON");

            // initially the chat box is in its "viewing" state
            _chatTextInputBox.SetActive(false);
            _chatSendMessageButton.SetActive(false);
            _chatWriteMessageButton.SetActive(true);
            _chatExitWritingMessageButton.SetActive(false);
            _chatScrollView.transform.position = new Vector3(10, 10, 0);

            // when we click the "write message" button, we want the chat to appear.
            _chatWriteMessageButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                // enable the input box.
                _chatTextInputBox.SetActive(true);

                // enable the input button.
                _chatSendMessageButton.SetActive(true);

                // enable the exit writing message button;
                _chatExitWritingMessageButton.SetActive(true);

                // disable the "open chat" button.
                _chatWriteMessageButton.SetActive(false);

                // move the chat box into position.
                _chatScrollView.transform.position = new Vector3(10, 100, 0);
            });

            // go back to the normal chat box view.
            _chatExitWritingMessageButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                // close the text input box.
                _chatTextInputBox.SetActive(false);

                // close the send message button.
                _chatSendMessageButton.SetActive(false);

                // show the "open chat" button.
                _chatWriteMessageButton.SetActive(true);

                //move the chat box back into the starting position
                _chatScrollView.transform.position = new Vector3(10, 10, 0);

                // close the exit writing button
                _chatExitWritingMessageButton.SetActive(false);
            });

            // when we press the send button
            _chatSendMessageButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                // get the text in the input box.
                string chatMessage = _chatTextInputBox.GetComponent<InputField>().text;

                state.Message = chatMessage;
            });


            state.AddCallback("Message", () =>
            {
                AddMessage(state.Message);
                UpdateChatBox();
            });
        }


        base.Attached();
    }

    public void AddMessage(string pMessage)
    {
        _messageLog.Add(pMessage);
    }

    private void UpdateChatBox()
    {
        // clear the current chat box.
        for (int i = 0; i < _chatMessageUIElements.Count; i++)
        {
            GameObject.Destroy(_chatMessageUIElements[i]);
        }



        // add the new messages
        int yPosition = 0;
        for (int i = _messageLog.Count-1; i >= 0; i--)
        {
            string message = _messageLog[i];

            GameObject chatMessageUIElement = GameObject.Instantiate(ChatMessagePrefab, _chatScrollViewContent.transform);

            chatMessageUIElement.transform.localPosition = new Vector3(10, 10 + yPosition, 0);
            chatMessageUIElement.GetComponent<Text>().text = message;

            _chatMessageUIElements.Add(chatMessageUIElement);
            yPosition += 32;
        }
    }

    public override void SimulateOwner()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            state.Message = "New Message!" + _messageLog.Count;
        }
    }
}
