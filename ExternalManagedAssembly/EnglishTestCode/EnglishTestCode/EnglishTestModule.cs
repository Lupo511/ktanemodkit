using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class EnglishTestModule : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public GameObject TopDisplay;
    public TextMesh TopText;
    public GameObject BottomDisplay;
    public TextMesh BottomText;
    public TextMesh OptionsText;
    public KMSelectable SubmitSelectable;
    public KMSelectable LeftSelectable;
    public KMSelectable RightSelectable;
    public Shader UnlitShader;

    private bool activated;
    private List<Question> questions;
    private int solvedQuestions;
    private int targetQuestions;
    private Question currentQuestion;
    private int selectedAnswerIndex;

    public void Start()
    {
        activated = false;
        solvedQuestions = 0;
        targetQuestions = 3;

        Module.OnActivate += OnActivate;
        SubmitSelectable.OnInteract += OnSubmitInteract;
        LeftSelectable.OnInteract += OnLeftInteract;
        RightSelectable.OnInteract += OnRightInteract;

        questions = new List<Question>();
        string[] lines = EnglishTestCode.Properties.Resources.setnences.Split('\n', '\r');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line))
                continue;
            if (line.StartsWith("//"))
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
    }

    public void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void OnActivate()
    {
        activated = true;

        selectQuestion();
    }

    private bool OnSubmitInteract()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitSelectable.transform);
        SubmitSelectable.AddInteractionPunch(1);
        if (!activated)
            return false;
        if (currentQuestion == null)
            return false;

        StartCoroutine(nextQuestion(selectedAnswerIndex == currentQuestion.CorrectAnswerIndex));
        return false;
    }

    private bool OnLeftInteract()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, LeftSelectable.transform);
        LeftSelectable.AddInteractionPunch(0.5f);
        if (!activated)
            return false;
        if (currentQuestion == null)
            return false;

        selectedAnswerIndex--;
        if (selectedAnswerIndex < 0)
            selectedAnswerIndex = currentQuestion.Answers.Count - 1;
        setBottomText(currentQuestion.QuestionText.Replace("[%]", "<i>" + currentQuestion.Answers[selectedAnswerIndex] + "</i>"));
        updateOptionsText();
        return false;
    }

    private bool OnRightInteract()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, RightSelectable.transform);
        RightSelectable.AddInteractionPunch(0.5f);
        if (!activated)
            return false;
        if (currentQuestion == null)
            return false;

        selectedAnswerIndex++;
        if (selectedAnswerIndex >= currentQuestion.Answers.Count)
            selectedAnswerIndex = 0;
        setBottomText(currentQuestion.QuestionText.Replace("[%]", "<i>" + currentQuestion.Answers[selectedAnswerIndex] + "</i>"));
        updateOptionsText();
        return false;
    }

    private IEnumerator nextQuestion(bool correct)
    {
        currentQuestion = null;
        BottomText.gameObject.SetActive(false);
        OptionsText.gameObject.SetActive(false);
        if (!correct)
        {
            Module.HandleStrike();
            solvedQuestions = 0;
            TopText.color = Color.red;
            TopText.text = "Incorrect";
        }
        else
        {
            solvedQuestions++;
            if (solvedQuestions == targetQuestions)
            {
                TopText.text = "Test passed";
                Module.HandlePass();
                yield break;
            }
            TopText.text = "Correct";
        }
        yield return new WaitForSeconds(3);
        TopText.color = Color.green;
        selectQuestion();
    }

    private void selectQuestion()
    {
        currentQuestion = questions[UnityEngine.Random.Range(0, questions.Count)];
        selectedAnswerIndex = UnityEngine.Random.Range(0, 100) > 15 ? 0 : UnityEngine.Random.Range(0, currentQuestion.Answers.Count);

        TopText.text = "Question " + (solvedQuestions + 1) + "/" + targetQuestions;
        TopText.gameObject.SetActive(true);

        setBottomText(currentQuestion.QuestionText.Replace("[%]", "<i>" + currentQuestion.Answers[selectedAnswerIndex] + "</i>"));
        BottomText.gameObject.SetActive(true);

        updateOptionsText();
        OptionsText.gameObject.SetActive(true);
    }

    private void setBottomText(string text)
    {
        float maxX = BottomDisplay.transform.localScale.x - 0.007f;
        //float maxZ = BottomDisplay.transform.localScale.z; unused

        BottomText.fontSize = 35;
        BottomText.text = text;

        string originalText = BottomText.text;
        while (getTextWidth(BottomText) > maxX)
        {
            string currentText = BottomText.text;
            List<int> indices = findAllIndicesOf(currentText, ' ');
            int i = indices.Count - 1;
            if (i >= 0)
            {
                while (getTextWidth(BottomText) > maxX)
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
                BottomText.text = originalText;
            }
            if (findAllIndicesOf(BottomText.text, '\n').Count > 3) //At most 4 lines
            {
                BottomText.fontSize--;
                BottomText.text = originalText;
            }
        }
    }

    private void updateOptionsText()
    {
        float maxX = BottomDisplay.transform.localScale.x - 0.007f;

        string unformattedText = "";
        int wordBeginIndex = 0;
        for (int i = 0; i < currentQuestion.Answers.Count; i++)
        {
            if (i > 0)
                unformattedText += " ";

            if (i == selectedAnswerIndex)
                wordBeginIndex = unformattedText.Length;

            unformattedText += currentQuestion.Answers[i];
        }

        OptionsText.fontSize = 35;
        float optionsTextWidth = 0;
        while (true)
        {
            optionsTextWidth = getTextWidth(unformattedText, OptionsText.font, OptionsText.fontSize, OptionsText.fontStyle) * OptionsText.characterSize;
            if (optionsTextWidth <= maxX)
                break;
            OptionsText.fontSize--;
        }

        float wordBegin = getTextWidth(unformattedText.Remove(wordBeginIndex), OptionsText.font, OptionsText.fontSize, OptionsText.fontStyle) * OptionsText.characterSize;
        float wordWidth = getTextWidth(currentQuestion.Answers[selectedAnswerIndex], OptionsText.font, OptionsText.fontSize, OptionsText.fontStyle) * OptionsText.characterSize;
        OptionsText.text = unformattedText.Insert(wordBeginIndex + currentQuestion.Answers[selectedAnswerIndex].Length, "</color>").Insert(wordBeginIndex, "<color=#000000>");

        if (OptionsText.transform.childCount > 0)
            Destroy(OptionsText.transform.GetChild(0).gameObject);
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Plane);
        background.name = "Highlight plane";
        background.transform.parent = OptionsText.gameObject.transform;
        background.transform.localPosition = new Vector3(wordBegin - (optionsTextWidth / 2) + (wordWidth / 2), 0, 0.000001f);
        background.transform.localScale = new Vector3(wordWidth * 0.1f, 1, getGameObjectSize(OptionsText.gameObject).y * 0.1f);
        background.transform.localRotation = Quaternion.Euler(new Vector3(-90, 0, 0));
        background.GetComponent<Renderer>().materials[0].shader = UnlitShader;
        background.GetComponent<Renderer>().materials[0].color = Color.green;
    }

    private float getTextWidth(TextMesh textMesh)
    {
        return getTextWidth(textMesh.text, textMesh.font, textMesh.fontSize, textMesh.fontStyle) * textMesh.characterSize;
    }

    private float getTextWidth(string text, Font font, int fontSize, FontStyle fontStyle)
    {
        float width = 0;
        foreach (string line in text.Split('\n', '\r'))
        {
            float lineWidth = 0;
            foreach (char c in line)
            {
                CharacterInfo characterInfo;
                if (font.GetCharacterInfo(c, out characterInfo, fontSize, fontStyle))
                {
                    lineWidth += characterInfo.advance;
                }
            }
            if (lineWidth > width)
                width = lineWidth;
        }
        return width * 0.1f;
    }

    private Vector3 getGameObjectSize(GameObject go)
    {
        BoxCollider col = go.AddComponent<BoxCollider>();
        Vector3 size = col.size;
        Destroy(col);
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
}
