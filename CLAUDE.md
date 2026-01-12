
================================================================================
                    MUSEUM QUIZ VR - DOCUMENTATION PROJET
================================================================================

Derniere mise a jour: 11 Janvier 2026

================================================================================
                              DESCRIPTION DU JEU
================================================================================

Museum Quiz VR est un jeu educatif en realite virtuelle pour Meta Quest.
Le joueur explore un musee virtuel contenant 31 tableaux de monuments celebres.
En interagissant avec un tableau, une question quiz est generee par l'API
Google Gemini. Le joueur doit repondre correctement pour gagner des points.

OBJECTIFS DU JEU:
- Atteindre un score cible (configurable, defaut: 500 points)
- OU completer un nombre de tableaux (configurable, defaut: 5 tableaux)
- Le tout avant la fin du temps imparti (configurable, defaut: 5 minutes)

================================================================================
                           ARCHITECTURE DU CODE
================================================================================

NAMESPACE: MuseumAI

DOSSIER: Assets/_Game/Scripts/

1. CORE (Scripts principaux)
   ------------------------
   - GameManager.cs
     * Singleton gerant l'etat global du jeu
     * Gere le score, le timer, les conditions de victoire
     * Etats: MainMenu, Playing, Paused, GameOver
     * Spawn le QuizUI et GameOverUI
     * Methodes cles:
       - StartGame() : Demarre une partie
       - EndGame(bool timeUp) : Termine la partie
       - StartQuizForPainting(PaintingController) : Lance un quiz
       - OnQuizAnswered(bool isCorrect) : Traite la reponse
       - SetGameSettings(int score, int paintings, float duration) : Configure les parametres

2. API (Communication avec Google Gemini)
   --------------------------------------
   - APIManager.cs
     * Singleton pour les appels API
     * Genere des quiz via Google Gemini
     * Methode principale: GenerateQuiz(monumentName, context, onSuccess, onError)

   - ApiConfig.cs (ScriptableObject)
     * Stocke la cle API et les parametres (temperature, max tokens)
     * Creer via: Create > Museum AI > Api Config

   - GeminiRequest.cs / GeminiResponse.cs
     * Classes de serialisation JSON pour l'API

   - QuizData.cs
     * Structure des donnees quiz (question, trueAnswer, falseAnswers)

3. GAMEPLAY
   --------
   - PaintingController.cs
     * Attache a chaque tableau
     * Contient: paintingTitle, artistName, year, paintingContext
     * Detecte l'interaction du joueur
     * GetFullContext() : Retourne le contexte pour l'API

   - PlayerInteraction.cs
     * Gere le raycast VR pour pointer sur les tableaux
     * Detecte les clics sur les boutons UI

4. UI (Interface Utilisateur)
   --------------------------
   - HUDController.cs
     * Affiche timer et score sur la main gauche (style montre)
     * S'abonne aux events du GameManager

   - QuizUIController.cs
     * Affiche la question et les 4 reponses
     * Gere la selection et validation des reponses

   - GameOverUIController.cs
     * Ecran de fin de partie
     * Boutons Rejouer et Quitter

   - MainMenuController.cs (NOUVEAU - A FINALISER)
     * Menu principal avec sliders de configuration
     * Permet de regler: score cible, nb tableaux, temps

   - FuturisticHUDStyle.cs / FuturisticGameOverStyle.cs / FuturisticMainMenuStyle.cs
     * Appliquent le style visuel cyan/holographique

================================================================================
                              PREFABS
================================================================================

Dossier: Assets/_Game/Prefabs/

- QuizPanel.prefab : Interface de quiz (spawn devant le joueur)
- GameOverPanel.prefab : Ecran de fin de partie
- MainMenuPanel.prefab : Menu principal (A FINALISER)

Configuration des Canvas VR:
- Render Mode: World Space
- Scale: 0.0005 a 0.001 (tres petit pour VR)
- Ajouter BoxCollider sur les boutons pour le raycast

================================================================================
                         CONFIGURATION UNITY
================================================================================

SCENE PRINCIPALE: Assets/Museum/Museum.unity

GAMEOBJECTS IMPORTANTS:
- _SYSTEMS : Contient GameManager et APIManager
- OVRCameraRig : Camera VR Meta Quest
- WristHUD : HUD attache a la main gauche
- Museum : Contient tous les tableaux et la geometrie

REFERENCES A ASSIGNER DANS GAMEMANAGER:
- Quiz Panel Prefab : QuizPanel.prefab
- Scene HUD : WristHUD (dans la scene)
- Game Over Prefab : GameOverPanel.prefab
- Main Menu Prefab : MainMenuPanel.prefab (NOUVEAU)
- Player Movement : Script de mouvement a desactiver en GameOver

================================================================================
                    CE QU'IL RESTE A FAIRE (TODO)
================================================================================

1. MENU PRINCIPAL (Priorite Haute)
   --------------------------------
   Probleme actuel: Le menu s'affiche mais la taille/position n'est pas correcte.

   A faire:
   [ ] Ajuster l'echelle du prefab MainMenuPanel (essayer 0.0003 ou 0.0002)
   [ ] Verifier que le Canvas est bien en World Space avec renderMode = 2
   [ ] Assigner les references dans MainMenuController:
       - scoreSlider, paintingsSlider, timeSlider
       - scoreValueText, paintingsValueText, timeValueText
       - startButton
   [ ] Assigner MainMenuPanel.prefab dans GameManager.mainMenuPrefab
   [ ] Tester le blocage du mouvement en MainMenu

2. COLLISION META XR SIMULATOR (Priorite Moyenne)
   -----------------------------------------------
   Probleme: Le joueur traverse les murs/tables en mode simulateur.

   A investiguer:
   [ ] Verifier les colliders sur la geometrie du musee
   [ ] Verifier la configuration du CharacterController/OVRPlayerController
   [ ] Peut etre specifique au simulateur (fonctionne peut-etre sur casque reel)

3. AMELIORATIONS FUTURES (Optionnel)
   ----------------------------------
   [ ] Ajouter des sons (bonne/mauvaise reponse, ambiance)
   [ ] Ajouter un tableau des scores (leaderboard)
   [ ] Ajouter plus de monuments/tableaux
   [ ] Mode multijoueur?

================================================================================
                         MONUMENTS DANS LE JEU
================================================================================

31 tableaux avec donnees completes (titre, artiste, annee, contexte):

1. Tour Eiffel (Paris)
2. Colisee (Rome)
3. Sagrada Familia (Barcelone)
4. Big Ben (Londres)
5. Statue de la Liberte (New York)
6. Pyramides de Gizeh (Egypte)
7. Grande Muraille de Chine
8. Machu Picchu (Perou)
9. Christ Redempteur (Rio)
10. Chichen Itza (Mexique)
11. Opera de Sydney
12. Petra (Jordanie)
13. Sainte-Sophie (Istanbul)
14. Chateau de Versailles
15. Louvre (Paris)
16. Notre-Dame de Paris
17. Mont-Saint-Michel
18. Sacre-Coeur (Paris)
19. Arc de Triomphe (Paris)
20. Champs-Elysees (Paris)
21. Pont du Gard
22. Arenes de Nimes
23. Cathedrale de Strasbourg
24. Chateau de Chambord
25. Chateau de Chenonceau
26. Cite de Carcassonne
27. Palais des Papes (Avignon)
28. Sainte-Chapelle (Paris)
29. Opera Garnier (Paris)
30. Pantheon (Paris)
31. Capitol (Washington)

================================================================================
                           NOTES TECHNIQUES
================================================================================

API GOOGLE GEMINI:
- Endpoint: generativelanguage.googleapis.com
- Modele: gemini-2.0-flash (configurable dans ApiConfig)
- Rate limit: ~15 requetes/minute (erreur 429 si depasse)
- Le prompt demande une question incluant le nom du monument

FORMAT DES QUIZ:
{
  "question": "En quelle annee la Tour Eiffel a-t-elle ete construite?",
  "trueAnswer": "1889",
  "falseAnswers": ["1900", "1875", "1920"]
}

STYLE VISUEL:
- Couleur principale: Cyan (#00E5FF)
- Background: Bleu fonce semi-transparent (0.02, 0.05, 0.1, 0.92)
- Effets: Outline glow sur les elements UI

================================================================================
                              FIN DU DOCUMENT
================================================================================
