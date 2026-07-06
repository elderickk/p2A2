using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Worm antagonist math challenge system — locks tiles and prompts math questions.
public class MathChallengeController : MonoBehaviour {

    public static MathChallengeController Instance { get; private set; }

    public enum ChallengeState { Inactive, Active, Correct, Wrong }
    public enum ChallengeType { Addition, Subtraction, Multiplication, ModuloBasic, Division }

    [SerializeField] private float challengeTimeout = 15f;
    [SerializeField] private int penaltyRotations = 1;
    [SerializeField] private bool regenerateOnWrong = true;

    private ChallengeState state = ChallengeState.Inactive;
    private CablePiece targetTile;

    private string _inputBuffer = "";
    private float _timeLeft;
    private int _correctAnswer;
    private string _questionText;
    private ChallengeType _currentType;

    public ChallengeState State => state;
    public ChallengeType CurrentType => _currentType;
    public CablePiece TargetTile => targetTile;
    public string questionText => _questionText;
    public string inputBuffer => _inputBuffer;
    public float timeLeft => _timeLeft;
    public float challengeTimeoutVal => challengeTimeout;

    public float ChallengeTimeout { get => challengeTimeout; set => challengeTimeout = value; }
    public int PenaltyRotations { get => penaltyRotations; set => penaltyRotations = value; }
    public bool RegenerateOnWrong { get => regenerateOnWrong; set => regenerateOnWrong = value; }

    // [NEW - MathUnlock]
    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    // [NEW - MathUnlock]
    private void Update() {
        if (state != ChallengeState.Active) {
            return;
        }

        foreach (char c in Input.inputString) {
            if (c == '\b') {
                if (_inputBuffer.Length > 0) {
                    _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                }
            } else if (char.IsDigit(c)) {
                if (_inputBuffer.Length < 4) {
                    _inputBuffer += c;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) {
            SubmitAnswer();
        }
    }

    // [NEW - MathUnlock]
    public void TriggerChallenge(CablePiece tile, int level) {
        Debug.Log($"[LCM] Math challenge triggered for level {level}. State: {state}");
        if (state == ChallengeState.Active) {
            return;
        }
        targetTile = tile;
        GenerateChallenge(level);
        _inputBuffer = "";
        _timeLeft = challengeTimeout;
        state = ChallengeState.Active;
        tile.SetLocked(true);
        StartCoroutine(ChallengeTimeoutCoroutine());
    }

    // [NEW - MathUnlock]
    private void GenerateChallenge(int level) {
        int maxVal = 10;
        List<ChallengeType> allowed = new List<ChallengeType> { ChallengeType.Addition, ChallengeType.Subtraction };

        if (level >= 2) {
            maxVal = 10;
            allowed.Add(ChallengeType.Multiplication);
            allowed.Add(ChallengeType.Division);
        }
        if (level >= 4) {
            maxVal = 12;
        }
        if (level >= 5) {
            maxVal = 20;
            allowed.Add(ChallengeType.ModuloBasic);
        }

        _currentType = allowed[Random.Range(0, allowed.Count)];
        int op1 = Random.Range(1, maxVal + 1);
        int op2 = Random.Range(1, maxVal + 1);

        switch (_currentType) {
            case ChallengeType.Addition:
                _questionText = $"{op1} + {op2} = ?";
                _correctAnswer = op1 + op2;
                break;
            case ChallengeType.Subtraction:
                if (op1 < op2) {
                    int temp = op1;
                    op1 = op2;
                    op2 = temp;
                }
                _questionText = $"{op1} - {op2} = ?";
                _correctAnswer = op1 - op2;
                break;
            case ChallengeType.Multiplication:
                _questionText = $"{op1} × {op2} = ?";
                _correctAnswer = op1 * op2;
                break;
            case ChallengeType.Division:
                int divisor = Random.Range(2, maxVal + 1);
                int quotient = Random.Range(1, maxVal + 1);
                int dividend = divisor * quotient;
                _questionText = $"{dividend} ÷ {divisor} = ?";
                _correctAnswer = quotient;
                break;
            case ChallengeType.ModuloBasic:
                int divisorMod = Random.Range(2, Mathf.Min(6, op1));
                _questionText = $"{op1} mod {divisorMod} = ?";
                _correctAnswer = op1 % divisorMod;
                break;
        }
    }

    // [NEW - MathUnlock]
    private IEnumerator ChallengeTimeoutCoroutine() {
        while (_timeLeft > 0 && state == ChallengeState.Active) {
            _timeLeft -= Time.deltaTime;
            yield return null;
        }
        if (state == ChallengeState.Active) {
            OnWrongAnswer();
        }
    }

    // [NEW - MathUnlock]
    private void SubmitAnswer() {
        if (int.TryParse(_inputBuffer, out int answer)) {
            if (answer == _correctAnswer) {
                OnCorrectAnswer();
            } else {
                OnWrongAnswer();
            }
        }
    }

    // [NEW - MathUnlock]
    private void OnCorrectAnswer() {
        state = ChallengeState.Correct;
        targetTile.SetLocked(false);
        targetTile.PlayUnlockEffect();
        if (ScreenShakeController.Instance != null) {
            ScreenShakeController.Instance.FOVPunch(62f, 0.15f);
        }
        ParticleSpawner.EmitSolve(targetTile.transform.position);
        if (UIOverlay.Instance != null) {
            UIOverlay.Instance.ShowMathResult(true, _correctAnswer);
        }

        // Unlock achievements via AchievementManager
        float timeTaken = challengeTimeout - _timeLeft;
        if (AchievementManager.Instance != null) {
            AchievementManager.Instance.UnlockAchievement("math_first_unlock");

            if (timeTaken <= 3.5f) {
                AchievementManager.Instance.UnlockAchievement("math_speed_unlock");
            }

            if (_currentType == ChallengeType.Multiplication) {
                AchievementManager.Instance.UnlockAchievement("math_mult_unlock");
            } else if (_currentType == ChallengeType.Division) {
                AchievementManager.Instance.UnlockAchievement("math_div_unlock");
            } else if (_currentType == ChallengeType.ModuloBasic) {
                AchievementManager.Instance.UnlockAchievement("math_mod_unlock");
            }

            AchievementManager.Instance.IncrementSolvesThisRound();
        }

        StartCoroutine(ResetAfter(0.8f));
    }

    // [NEW - MathUnlock]
    private void OnWrongAnswer() {
        state = ChallengeState.Wrong;
        if (ScreenShakeController.Instance != null) {
            ScreenShakeController.Instance.GlitchShake();
        }
        if (UIOverlay.Instance != null) {
            UIOverlay.Instance.ShowMathResult(false, _correctAnswer);
        }
        if (WormController.Instance != null) {
            WormController.Instance.PunishWrongAnswer();
        }
        StartCoroutine(ResetAfter(1.0f));
    }

    // [NEW - MathUnlock]
    private IEnumerator ResetAfter(float t) {
        yield return new WaitForSeconds(t);
        ChallengeState oldState = state;
        if (state != ChallengeState.Active) {
            state = ChallengeState.Inactive;
            _inputBuffer = "";
        }
        if (oldState == ChallengeState.Wrong && regenerateOnWrong && targetTile != null && targetTile.IsLocked) {
            int level = GameManager.Instance != null ? GameManager.Instance.CurrentRound : 1;
            TriggerChallenge(targetTile, level);
        }
    }
}
