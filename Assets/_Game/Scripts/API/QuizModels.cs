using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuseumAI.API
{
    // MODELES DE DONNEES POUR LE QUIZ

    /// <summary>
    /// Donnees du quiz generees par l'IA
    /// </summary>
    [Serializable]
    public class QuizData
    {
        public string question;
        public string trueAnswer;
        public string[] falseAnswers;
        public string historicalFact; // Fait historique interessant sur le monument

        /// <summary>
        /// Genere une liste de choix melanges avec indication de la bonne reponse
        /// </summary>
        public List<QuizChoice> GetShuffledChoices()
        {
            List<QuizChoice> choices = new List<QuizChoice>();

            choices.Add(new QuizChoice
            {
                text = trueAnswer,
                isCorrect = true
            });

            if (falseAnswers != null)
            {
                foreach (string falseAnswer in falseAnswers)
                {
                    choices.Add(new QuizChoice
                    {
                        text = falseAnswer,
                        isCorrect = false
                    });
                }
            }

            UnityEngine.Random.InitState((int)(System.DateTime.Now.Ticks % int.MaxValue));

            for (int i = choices.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                QuizChoice temp = choices[i];
                choices[i] = choices[randomIndex];
                choices[randomIndex] = temp;
            }


            return choices;
        }

        /// <summary>
        /// Valide que les donnees du quiz sont completes
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(question) &&
                   !string.IsNullOrEmpty(trueAnswer) &&
                   falseAnswers != null &&
                   falseAnswers.Length >= 3;
        }
    }

    /// <summary>
    /// Represente un choix de reponse dans le quiz
    /// </summary>
    [Serializable]
    public class QuizChoice
    {
        public string text;
        public bool isCorrect;
    }

    // WRAPPERS POUR L'API GOOGLE GEMINI

    #region Gemini Request Models

    /// <summary>
    /// Corps de la requete envoyee a l'API Gemini
    /// </summary>
    [Serializable]
    public class GeminiRequest
    {
        public GeminiContent[] contents;
        public GeminiGenerationConfig generationConfig;

        /// <summary>
        /// Constructeur avec valeurs par defaut
        /// </summary>
        public GeminiRequest(string prompt) : this(prompt, 0.7f, 2048)
        {
        }

        /// <summary>
        /// Constructeur avec parametres de configuration personnalises
        /// </summary>
        /// <param name="prompt">Le prompt a envoyer</param>
        /// <param name="temperature">Creativite (0.0 = deterministe, 1.0 = creatif)</param>
        /// <param name="maxOutputTokens">Nombre max de tokens dans la reponse</param>
        public GeminiRequest(string prompt, float temperature, int maxOutputTokens)
        {
            contents = new GeminiContent[]
            {
                new GeminiContent
                {
                    parts = new GeminiPart[]
                    {
                        new GeminiPart { text = prompt }
                    }
                }
            };

            generationConfig = new GeminiGenerationConfig
            {
                temperature = temperature,
                topK = 40,
                topP = 0.95f,
                maxOutputTokens = maxOutputTokens
            };
        }
    }

    [Serializable]
    public class GeminiContent
    {
        public string role;
        public GeminiPart[] parts;
    }

    [Serializable]
    public class GeminiPart
    {
        public string text;
    }

    [Serializable]
    public class GeminiGenerationConfig
    {
        public float temperature;
        public int topK;
        public float topP;
        public int maxOutputTokens;
    }

    #endregion

    #region Gemini Response Models

    /// <summary>
    /// Reponse complete de l'API Gemini
    /// </summary>
    [Serializable]
    public class GeminiResponse
    {
        public GeminiCandidate[] candidates;
        public GeminiUsageMetadata usageMetadata;
        public GeminiError error;

        /// <summary>
        /// Extrait le texte de la premiere reponse
        /// </summary>
        public string GetResponseText()
        {
            if (candidates != null &&
                candidates.Length > 0 &&
                candidates[0].content != null &&
                candidates[0].content.parts != null &&
                candidates[0].content.parts.Length > 0)
            {
                return candidates[0].content.parts[0].text;
            }
            return null;
        }

        /// <summary>
        /// Verifie si la reponse contient une erreur
        /// </summary>
        public bool HasError()
        {
            return error != null && !string.IsNullOrEmpty(error.message);
        }
    }

    [Serializable]
    public class GeminiCandidate
    {
        public GeminiContent content;
        public string finishReason;
        public int index;
        public GeminiSafetyRating[] safetyRatings;
    }

    [Serializable]
    public class GeminiSafetyRating
    {
        public string category;
        public string probability;
    }

    [Serializable]
    public class GeminiUsageMetadata
    {
        public int promptTokenCount;
        public int candidatesTokenCount;
        public int totalTokenCount;
    }

    [Serializable]
    public class GeminiError
    {
        public int code;
        public string message;
        public string status;
    }

    #endregion
}
