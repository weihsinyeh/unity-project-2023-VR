using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Linq;

public class VoiceRecognizerPortal : MonoBehaviour
{
    public ControlUI controlUI; // In portal game
    public PortalPlacement portalPlacement;
    KeywordRecognizer keywordRecognizer;
    Dictionary<string,Action> keywords = new Dictionary<string, Action>();

    // Start is called before the first frame update
    void Start()
    {
        //Create keywords for keyword recognizer
        keywords.Add("Start", controlUI.startGame);
        keywords.Add("Show", controlUI.showMap);
        keywords.Add("In", portalPlacement.PlaceIn);
        keywords.Add("Out", portalPlacement.PlaceOut);
        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray(),ConfidenceLevel.Low);
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        keywordRecognizer.Start();
    }


    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Action keywordAction;
        // if the keyword recognized is in our dictionary, call that Action.
        if (keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }

}