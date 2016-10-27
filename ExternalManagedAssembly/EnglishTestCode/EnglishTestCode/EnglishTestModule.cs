using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class EnglishTestModule : MonoBehaviour
{
    private KMBombModule module;
    private bool activated;
    private List<Question> questions;
    private Question currentQuestion;
    private int currentAnswerIndex;

    public void Start()
    {
        activated = false;

        module = GetComponent<KMBombModule>();
        module.OnActivate += OnActivate;

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

        currentQuestion = questions[Random.Range(0, questions.Count)];
        currentAnswerIndex = Random.Range(0, currentQuestion.Answers.Count);

        findChildGameObjectByName(gameObject, "Top Text").GetComponent<TextMesh>().text = "Question 1/3";
        findChildGameObjectByName(gameObject, "Top Text").SetActive(true);
        findChildGameObjectByName(gameObject, "Bottom Text").GetComponent<TextMesh>().text = currentQuestion.QuestionText.Replace("%", currentQuestion.Answers[currentAnswerIndex]);
        findChildGameObjectByName(gameObject, "Bottom Text").SetActive(true);
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
}
