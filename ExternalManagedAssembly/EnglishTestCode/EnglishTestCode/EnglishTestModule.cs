using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class EnglishTestModule : MonoBehaviour
{
    private KMBombModule module;
    private GameObject topDisplay;
    private TextMesh topText;
    private GameObject bottomDisplay;
    private TextMesh bottomText;

    private bool activated;
    private List<Question> questions;
    private int solvedQuestions;
    private int targetQuestions;
    private Question currentQuestion;
    private int currentAnswerIndex;

    public void Start()
    {
        activated = false;
        solvedQuestions = 0;
        targetQuestions = 3;

        Module.OnActivate += OnActivate;
        findChildGameObjectByName(gameObject, "Submit Button").GetComponent<KMSelectable>().OnInteract += OnSubmitInteract;
        findChildGameObjectByName(gameObject, "Left Button").GetComponent<KMSelectable>().OnInteract += OnLeftInteract;
        findChildGameObjectByName(gameObject, "Right Button").GetComponent<KMSelectable>().OnInteract += OnRightInteract;

        questions = new List<Question>();
        string[] lines = EnglishTestCode.Properties.Resources.setnences.Split('\n', '\r');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line))
                continue;
            Regex regex = new Regex(@"(?<=\[).*(?=\])");
            Match match = regex.Match(line);
            if (match.Success)
            {
                Question question = new Question();
                question.QuestionText = line.Replace(match.Value, "%");
                string[] answers = match.Value.Split('|');
                for (int j = 0; j < answers.Length; j++)
                {
                    string answer = answers[j];
                    if (answer.StartsWith("!"))
                    {
                        question.CorrectAnswerIndex = j;
                        answer = answer.Substring(1);
                    }
                    question.Answers.Add(answer);
                }
                questions.Add(question);
            }
            else
            {
                Debug.Log("Couldn't find options match for string at line " + i);
            }
        }

#if !UNITY_EDITOR
        foreach (MonoBehaviour component in findChildGameObjectByName(gameObject, "Submit Button").GetComponents<MonoBehaviour>())
        {
            if (component.GetType().FullName == "ModSelectable")
            {
                component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
            }
        }
        foreach (MonoBehaviour component in findChildGameObjectByName(gameObject, "Left Button").GetComponents<MonoBehaviour>())
        {
            if (component.GetType().FullName == "ModSelectable")
            {
                component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
            }
        }
        foreach (MonoBehaviour component in findChildGameObjectByName(gameObject, "Right Button").GetComponents<MonoBehaviour>())
        {
            if (component.GetType().FullName == "ModSelectable")
            {
                component.GetType().BaseType.GetField("ForceInteractionHighlight", BindingFlags.Public | BindingFlags.Instance).SetValue(component, true);
            }
        }
#endif
    }

    private void OnActivate()
    {
        activated = true;

        selectQuestion();
    }

    private bool OnSubmitInteract()
    {
        selectQuestion();
        return false;
    }

    private bool OnLeftInteract()
    {
        return false;
    }

    private bool OnRightInteract()
    {
        return false;
    }

    private void selectQuestion()
    {
        currentQuestion = questions[UnityEngine.Random.Range(0, questions.Count)];
        currentAnswerIndex = 0;

        TopText.text = "Question " + (solvedQuestions + 1) + "/" + targetQuestions;
        TopText.gameObject.SetActive(true);

        setBottomText(currentQuestion.QuestionText.Replace("[%]", "<i>" + currentQuestion.Answers[currentAnswerIndex] + "</i>"));
        BottomText.gameObject.SetActive(true);
    }

    private void setBottomText(string text)
    {
        float maxX = BottomDisplay.AddComponent<BoxCollider>().size.x * BottomDisplay.transform.localScale.x;
        float maxZ = BottomDisplay.GetComponent<BoxCollider>().size.z * BottomDisplay.transform.localScale.z;
        BottomText.fontSize = 35;
        BottomText.text = text;
        string originalText = BottomText.text;
        while (getGameObjectSize(BottomText.gameObject).x > maxX)
        {
            string currentText = BottomText.text;
            List<int> indices = findAllIndicesOf(currentText, ' ');
            int i = indices.Count - 1;
            if (i >= 0)
            {
                while (getGameObjectSize(BottomText.gameObject).x > maxX)
                {
                    BottomText.text = currentText.Substring(0, indices[i]);
                    i--;
                    if (i < 0)
                        break;
                }
            }
            if (i >= 0)
            {
                BottomText.text += "\n" + currentText.Substring(indices[i + 1] + 1);
            }
            else
            {
                BottomText.fontSize--;
            }
            if (findAllIndicesOf(BottomText.text, '\n').Count > 4)
            {
                BottomText.fontSize--;
                BottomText.text = originalText;
            }
        }
        BottomText.gameObject.AddComponent<BoxCollider>();
    }

    private Vector3 getGameObjectSize(GameObject go)
    {
        Vector3 size = go.AddComponent<BoxCollider>().size;
        Destroy(go.GetComponent<BoxCollider>()); //Check not working
        return size;
    }

    private List<int> findAllIndicesOf(string str, char chr)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == chr)
                indices.Add(i);
        }
        return indices;
    }

    private GameObject findChildGameObjectByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.name == name)
                return child.gameObject;
            GameObject childGo = findChildGameObjectByName(child.gameObject, name);
            if (childGo != null)
                return childGo;
        }
        return null;
    }

    public KMBombModule Module
    {
        get
        {
            if (module == null)
                module = GetComponent<KMBombModule>();
            return module;
        }
    }

    public GameObject TopDisplay
    {
        get
        {
            if (topDisplay == null)
                topDisplay = findChildGameObjectByName(gameObject, "Top Display");
            return topDisplay;
        }
    }

    public TextMesh TopText
    {
        get
        {
            if (topText == null)
                topText = findChildGameObjectByName(gameObject, "Top Text").GetComponent<TextMesh>();
            return topText;
        }
    }

    public GameObject BottomDisplay
    {
        get
        {
            if (bottomDisplay == null)
                bottomDisplay = findChildGameObjectByName(gameObject, "Bottom Display");
            return bottomDisplay;
        }
    }

    public TextMesh BottomText
    {
        get
        {
            if (bottomText == null)
                bottomText = findChildGameObjectByName(gameObject, "Bottom Text").GetComponent<TextMesh>();
            return bottomText;
        }
    }
}
