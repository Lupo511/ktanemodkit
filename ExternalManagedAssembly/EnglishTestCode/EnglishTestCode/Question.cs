using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Question
{
    private string questionText;
    private List<string> answers;
    private int correctAnswerIndex;

    public Question()
    {
        questionText = "";
        answers = new List<string>();
        correctAnswerIndex = -1;
    }

    public string QuestionText
    {
        get
        {
            return questionText;
        }

        set
        {
            questionText = value;
        }
    }

    public List<string> Answers
    {
        get
        {
            return answers;
        }

        set
        {
            answers = value;
        }
    }

    public int CorrectAnswerIndex
    {
        get
        {
            return correctAnswerIndex;
        }

        set
        {
            correctAnswerIndex = value;
        }
    }
}