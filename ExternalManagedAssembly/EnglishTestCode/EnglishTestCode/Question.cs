using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Question
{
    public string QuestionText { get; set; } = "";
    public int AnswerTextIndex { get; set; } = -1;
    public List<string> Answers { get; set; } = new List<string>();
    public int CorrectAnswerIndex { get; set; } = -1;
}