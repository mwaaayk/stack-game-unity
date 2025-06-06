using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerBlockPrefab;
    [SerializeField] private GameObject _stackedBlockPrefab;
    [SerializeField] private GameObject _perfectEffectPrefab;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _moveBounds = 5f;
    [SerializeField] private GameObject _camera;
    [SerializeField] private UIManager _UIManager;
    [SerializeField] private int _initialPlayerBlockPoolSize = 20;
    [SerializeField] private int _initialStackedBlockPoolSize = 20;

    private Queue<GameObject> _playerBlockPool = new Queue<GameObject>();
    private Queue<GameObject> _stackedBlockPool = new Queue<GameObject>();

    private int _score = 0;
    private GameObject _currentBlock;
    private Vector3 _lastBlockPosition;
    private Vector3 _lastBlockScale;
    private bool _isMovingPositive = true;
    private bool _moveAlongX = true;
    private Vector3 _startingCameraPosition;
    private bool _isNewGame = true;
    private List<GameObject> _spawnedBlocks = new List<GameObject>();

    private const float _perfectPlacementThreshold = 0.95f;

    private void Awake()
    {
        _startingCameraPosition = _camera.transform.position;
        InitializePools();
    }

    private void OnEnable() => InitializeGame();

    private void InitializeGame()
    {
        _score = 0;
        _lastBlockPosition = Vector3.zero;
        _lastBlockScale = new Vector3(2, 0.5f, 2);
        SpawnNewBlock();
    }

    private void Update()
    {
        if (_currentBlock == null) return;

        HandleBlockMovement();

        if (Input.GetMouseButtonDown(0))
        {
            PlaceCurrentBlock();

            if (_isNewGame)
            {
                _UIManager.HideStartPanel();
                _isNewGame = false;
            }
        }
    }

    #region OBJECT POOLING

    private void InitializePools()
    {
        for (int i = 0; i < _initialPlayerBlockPoolSize; i++)
        {
            GameObject block = Instantiate(_playerBlockPrefab);
            block.SetActive(false);
            _playerBlockPool.Enqueue(block);
        }

        for (int i = 0; i < _initialStackedBlockPoolSize; i++)
        {
            GameObject block = Instantiate(_stackedBlockPrefab);
            block.SetActive(false);
            _stackedBlockPool.Enqueue(block);
        }
    }

    private GameObject GetPlayerBlockFromPool()
    {
        if (_playerBlockPool.Count > 0)
        {
            GameObject block = _playerBlockPool.Dequeue();
            block.SetActive(true);
            return block;
        }
        return Instantiate(_playerBlockPrefab);
    }

    private GameObject GetStackedBlockFromPool()
    {
        if (_stackedBlockPool.Count > 0)
        {
            GameObject block = _stackedBlockPool.Dequeue();
            block.SetActive(true);
            return block;
        }
        return Instantiate(_stackedBlockPrefab);
    }

    private void ReturnPlayerBlockToPool(GameObject block)
    {
        Rigidbody rb = block.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        block.SetActive(false);
        _playerBlockPool.Enqueue(block);
    }

    private void ReturnStackedBlockToPool(GameObject block)
    {
        Rigidbody rb = block.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        block.SetActive(false);
        _stackedBlockPool.Enqueue(block);
    }

    #endregion

    #region CORE MECHANICS

    private void SpawnNewBlock()
    {
        Vector3 spawnOffset = _moveAlongX
            ? new Vector3(_moveBounds, 0, 0)  // Always spawn from right
            : new Vector3(0, 0, _moveBounds); // Always spawn from forward

        Vector3 spawnPosition = _lastBlockPosition + new Vector3(0, _lastBlockScale.y, 0) + spawnOffset;

        _currentBlock = GetPlayerBlockFromPool();
        _currentBlock.transform.rotation = Quaternion.identity;
        _currentBlock.transform.position = spawnPosition;
        _currentBlock.transform.localScale = _lastBlockScale;
    }


    private void PlaceCurrentBlock()
    {
        float currentPos = _moveAlongX ? _currentBlock.transform.position.x : _currentBlock.transform.position.z;
        float currentSize = _moveAlongX ? _currentBlock.transform.localScale.x : _currentBlock.transform.localScale.z;
        float lastPos = _moveAlongX ? _lastBlockPosition.x : _lastBlockPosition.z;
        float lastSize = _moveAlongX ? _lastBlockScale.x : _lastBlockScale.z;

        float overlap = CalculateOverlap(currentPos, currentSize, lastPos, lastSize);

        if (overlap <= 0)
        {
            GameOver();
            return;
        }

        bool isPerfectPlacement = overlap >= currentSize * _perfectPlacementThreshold;

        Vector3 newScale = _lastBlockScale;
        if (!isPerfectPlacement)
        {
            if (_moveAlongX)
                newScale.x = overlap;
            else
                newScale.z = overlap;
        }

        Vector3 newPosition = _lastBlockPosition;
        float dir = currentPos > lastPos ? 1 : -1;
        float offset = ((currentSize - overlap) / 2f) * dir;

        if (_moveAlongX)
            newPosition.x += offset;
        else
            newPosition.z += offset;

        newPosition.y = _currentBlock.transform.position.y;

        SpawnStackedBlock(newPosition, newScale, isPerfectPlacement);

        _lastBlockPosition = newPosition;
        _lastBlockScale = newScale;
        UpdateCameraPosition();

        DestroyAndReplaceCurrentBlock();
    }

    private void DestroyAndReplaceCurrentBlock()
    {
        ReturnPlayerBlockToPool(_currentBlock);
        _isMovingPositive = !_isMovingPositive;
        _moveAlongX = !_moveAlongX; // Toggle axis after every block
        SpawnNewBlock();
    }

    private void HandleBlockMovement()
    {
        float movement = _moveSpeed * Time.deltaTime;

        // Choose axis and direction based on _isMovingPositive
        Vector3 direction;
        if (_moveAlongX)
            direction = _isMovingPositive ? Vector3.right : Vector3.left;
        else
            direction = _isMovingPositive ? Vector3.forward : Vector3.back;

        _currentBlock.transform.Translate(direction * movement);

        // Get position along the correct axis
        float pos = _moveAlongX ? _currentBlock.transform.position.x : _currentBlock.transform.position.z;

        // Flip direction when bounds are exceeded
        if (pos >= _moveBounds)
            _isMovingPositive = false;
        else if (pos <= -_moveBounds)
            _isMovingPositive = true;
    }


    private void SpawnStackedBlock(Vector3 position, Vector3 scale, bool isPerfect)
    {
        GameObject stackedBlock = GetStackedBlockFromPool();
        stackedBlock.transform.rotation = Quaternion.identity;
        stackedBlock.transform.position = position;
        stackedBlock.transform.localScale = scale;
        _spawnedBlocks.Add(stackedBlock);

        if (isPerfect)
        {
            _score += 2;
            Instantiate(_perfectEffectPrefab, position, _perfectEffectPrefab.transform.rotation);
        }
        else
        {
            _score++;
            CreateFallingPieces(_currentBlock.transform.position, _currentBlock.transform.localScale, scale);
        }

        _UIManager.UpdateScoreDisplay(_score);
    }

    private void UpdateCameraPosition() => _camera.transform.position += new Vector3(0, 0.5f, 0);

    #endregion

    #region BLOCK TRIMMING

    private float CalculateOverlap(float currentPos, float currentSize, float lastPos, float lastSize)
    {
        float currentMin = currentPos - currentSize / 2f;
        float currentMax = currentPos + currentSize / 2f;
        float lastMin = lastPos - lastSize / 2f;
        float lastMax = lastPos + lastSize / 2f;

        float overlapMin = Mathf.Max(currentMin, lastMin);
        float overlapMax = Mathf.Min(currentMax, lastMax);

        return Mathf.Max(0, overlapMax - overlapMin);
    }

    private void CreateFallingPieces(Vector3 originalPos, Vector3 originalScale, Vector3 newScale)
    {
        float cutSize = _moveAlongX ? (originalScale.x - newScale.x) : (originalScale.z - newScale.z);
        bool cutFromPositive = (_moveAlongX ? originalPos.x : originalPos.z) > (_moveAlongX ? _lastBlockPosition.x : _lastBlockPosition.z);

        Vector3 fallingPos = originalPos;
        float offset = (newScale.x / 2f + cutSize / 2f);

        if (_moveAlongX)
            fallingPos.x += cutFromPositive ? offset : -offset;
        else
            fallingPos.z += cutFromPositive ? offset : -offset;

        Vector3 fallingScale = new Vector3(
            _moveAlongX ? cutSize : originalScale.x,
            originalScale.y,
            _moveAlongX ? originalScale.z : cutSize
        );

        CreateFallingBlock(fallingPos, fallingScale, cutFromPositive);
    }

    private void CreateFallingBlock(Vector3 position, Vector3 scale, bool cutFromPositive)
    {
        GameObject fallingPiece = GetStackedBlockFromPool();
        fallingPiece.transform.rotation = Quaternion.identity;
        fallingPiece.transform.position = position;
        fallingPiece.transform.localScale = scale;
        _spawnedBlocks.Add(fallingPiece);

        Rigidbody rb = fallingPiece.AddComponent<Rigidbody>();
        Vector3 forceDir = _moveAlongX ? (cutFromPositive ? Vector3.right : Vector3.left)
                                       : (cutFromPositive ? Vector3.forward : Vector3.back);
        rb.AddForce(forceDir * 2f, ForceMode.Impulse);
    }

    #endregion

    private void GameOver()
    {
        CreateFallingBlock(_currentBlock.transform.position, _currentBlock.transform.localScale,
            _moveAlongX ? _currentBlock.transform.position.x > _lastBlockPosition.x
                        : _currentBlock.transform.position.z > _lastBlockPosition.z);
        ReturnPlayerBlockToPool(_currentBlock);
        gameObject.SetActive(false);
        _UIManager.ShowGameOver();
    }

    public void ResetGame()
    {
        enabled = true;
        _isNewGame = true;

        _moveAlongX = true;         // Always start moving along X
        _isMovingPositive = false;

        foreach (GameObject obj in _spawnedBlocks)
        {
            ReturnStackedBlockToPool(obj);
        }
        _spawnedBlocks.Clear();
        _camera.transform.position = _startingCameraPosition;
    }
}
