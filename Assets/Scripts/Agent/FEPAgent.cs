using UnityEngine;
using SimpleNN.Graph;
using SimpleNN.Optimizer;
using SimpleNN.Module;
using System;
using System.Collections;
using SimpleNN.Tensor;
using UnityEngine.Events;
using System.Collections.Generic;
/// <summary>
/// Main Agent class implementing the Free Energy Principle RL loop.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FEPAgent : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Range(10,300)] private int _updateIntervalFrames = 60;
    [SerializeField, Range(10,1500)] private int _globalUpdateIntervalFrames = 300;
    private int _globalBatchSize;
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private FEPAction _actionInterface;
    public int TopK
    {
        get
        {
            return ((FEPOscAction)_actionInterface).TopK;
        }
        set
        {
            ((FEPOscAction)_actionInterface).TopK = value;
        }
    }
    public FEPOscAction.ActionData[] ActionDatas => ((FEPOscAction)_actionInterface).ActionDatas;
    public void UpdateActionData(int index, Func<FEPOscAction.ActionData,FEPOscAction.ActionData> op) => ((FEPOscAction)_actionInterface).UpdateActionData(index,op);
    [SerializeField] private WebCamRenderer _webCamRenderer;
    public string WebcamDeviceName
    {
        get{return _webCamRenderer.DeviceName;}
        set{
            if (_useWebCam)
            {
                SetWebcamActive(false);
                _webCamRenderer.DeviceName = value;
                SetWebcamActive(true);
            }else{
                _webCamRenderer.DeviceName = value;
            }
        }
    }
    [SerializeField] private RenderTexture _renderTexture;
    [SerializeField] private bool _useWebCam = true;

    [Header("Audio")]
    [SerializeField] private int _nMels = 80;
    [SerializeField, Range(0.5f, 10f)] private float _bufferLength = 0.5f;
    [SerializeField,MicrophoneDevice] private string _micDeviceName = "";
    public string MicDeviceName
    {
        get{return _micDeviceName;}
        set{
            if (_useMicrophone)
            {
                SetMicrophoneActive(false);
                _micDeviceName = value;
                SetMicrophoneActive(true);
            }else{
                _micDeviceName = value;
            }
        }
    }
    public int AudioFeatureSize { get{ return _nMels; } }
    private int _sampleRate;
    private int _bufferSize;
    private float[] _audioBuffer;
    private Tensor _audioBufferTensor;
    private AudioSource _audioSource;
    private TensorMelSpectrogram _melSpectrogram;

    [Header("Hyperparameters")]
    [SerializeField] private float _learningRate = 0.001f;
    [SerializeField] private float _rewardMovingAvgFactor = 0.01f;

    private FEPImageEmbedding _embeddingImageModel;
    private FEPAudioEmbedding _embeddingAudioModel;
    private FEPActor _actorModel;

    // Global Models
    private FEPGlobalImageEmbedding _globalEmbeddingImageModel;
    private FEPGlobalAudioEmbedding _globalEmbeddingAudioModel;
    private FEPGlobalActor _globalActorModel;

    private Loss _mseLoss;
    private Adadelta _embeddingOptimizer;
    private Adadelta _actorOptimizer;
    
    // Global Optimizers
    private Adadelta _globalEmbeddingOptimizer;
    private Adadelta _globalActorOptimizer;

    private Texture2D _texture2D;
    private Tensor _textureTensor;
    private Tensor _actionLearnMask;
    private TensorBox _lastEmbeddedImageState;
    private TensorBox _lastEmbeddedAudioState;
    private TensorBox _lastLogProb;
    private float[] _lastLogProbData;
    private float[] _lastActionsData;
    public float[] LastEmbeddedImageData => _lastEmbeddedImageState.GetTensor().GetContiguousData();
    public float[] LastEmbededAudioData => _lastEmbeddedAudioState.GetTensor().GetContiguousData();
    public float[] LastLogProbData => _lastLogProbData;
    public float[] LastActionsData => _lastActionsData;
    
    // Global State
    private TensorBox _lastGlobalEmbeddedImageState;
    private TensorBox _lastGlobalEmbeddedAudioState;
    private TensorBox _lastGlobalLogProb;
    private Tensor[] _globalAudioBuffer;
    private Tensor[] _globalImageBuffer;
    //private Tensor[] _actionGlobalMaskBuffer;
    private int _globalBufferIdx = 0;
    private float _movingAvgReward = 0.0f;
    public float AvgReward => _movingAvgReward;
    private int _currentFrames = 0;
    private bool _isReady = false;
    public bool IsReady => _isReady;
    private bool _inferencing = false;
    private bool _switchQueued = false;
    private bool _paused = false;
    bool _isRunning = true;

    public void SetPause(bool value)
    {
        _paused = value;
    }

    public AudioSource GetAudioSource() => _audioSource;

    private bool _useMicrophone = false;
    private string _activatedMicrophone = "";
    public bool UseMicrophone => _useMicrophone;
    public bool UseWebcam => _useWebCam;
    public bool IsPaused => _paused;
    public UnityEvent onAIUpdated = new();

    void Awake()
    {
        _isReady = false;
        _inferencing = false;
        _paused = false;
        _sampleRate = AudioSettings.outputSampleRate;
        _bufferSize = (int)(_sampleRate * _bufferLength);
        _audioSource = GetComponent<AudioSource>();
        _audioBuffer = new float[_bufferSize];
        _audioBufferTensor = _audioBuffer;
    }

    public void RestartAudio()
    {
        if(_audioSource.isPlaying) _audioSource.Stop();
        _audioSource.Play();
    }

    public void SetAudio(AudioClip clip)
    {
        if (_audioSource.clip == clip) return;
        SetMicrophoneActive(false);
        if(_audioSource.isPlaying) _audioSource.Stop();
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    public void SetMicrophoneActive(bool active)
    {
        if (!_isReady) return;
        if (!(active ^ _useMicrophone)) return;
        if(_audioSource.isPlaying) _audioSource.Stop();
        if (_useMicrophone)
        {
            _useMicrophone = false;
            Microphone.End(_activatedMicrophone);
            _audioSource.loop = false;
            _audioSource.clip = null;
        }
        else
        {
            if (_switchQueued) return;
            _switchQueued = true;
            _useMicrophone = true;
            _isReady = false;
            _activatedMicrophone = _micDeviceName;
            _audioSource.clip = Microphone.Start(_micDeviceName, true, 5, AudioSettings.outputSampleRate);
            while (!(Microphone.GetPosition(_micDeviceName) > 0)) { }
            _audioSource.loop = true;
            _audioSource.Play();
            _isReady = true;
            _switchQueued = false;
        }
    }

    public void SetWebcamActive(bool active)
    {
        if (!_isReady) return;
        if (!(active ^ _useWebCam)) return;
        if (_switchQueued) return;

        _switchQueued = true;

        if(_webCamRenderer != null && _webCamRenderer.IsReady) _webCamRenderer.Stop();
        
        if (_useWebCam)
        {
            _useWebCam = false;
            ResetLearn(() =>
            {
                _switchQueued = false;
            });
        }
        else
        {
            _useWebCam = true;
            ResetLearn(() =>
            {
                _switchQueued = false;
            });
        }
    }

    IEnumerator ResetLearnAsync(Action onEnd = null)
    {
        _isReady = false;
        _paused = false;
        _useWebCam = _useWebCam && (_webCamRenderer is not null);
        while (_inferencing)
        {
            yield return null;
        }
        if (_useWebCam)
        {
            _webCamRenderer.renderTexture = _renderTexture;
            _webCamRenderer.Play();
        }
        else if(_webCamRenderer != null && _webCamRenderer.IsReady)
        {
            _webCamRenderer.Stop();
        }
        yield return SetupTexture();
        onEnd?.Invoke();
    }

    public void ResetLearn(Action onEnd = null)
    {
        StartCoroutine(ResetLearnAsync(onEnd));
    }

    private void Start()
    {
        _globalBatchSize = Mathf.CeilToInt((float)_globalUpdateIntervalFrames / _updateIntervalFrames);
        ResetLearn();
        StartCoroutine(ActionLoop());
    }

    IEnumerator SetupTexture()
    {
        while (_inferencing)
        {
            yield return null;
        }
        if (_useWebCam){
            while(!_webCamRenderer.IsReady){
                yield return null;
            }
        }
        else
        {
            if (_camera == null) _camera = Camera.main;
        }
        _texture2D = new Texture2D(_renderTexture.height, _renderTexture.width, TextureFormat.RGBA32, false);
        _textureTensor = Tensor.Unsqueeze(Tensor.AsConicalHSV(_texture2D),0);
        Debug.Log(_textureTensor);
        int frameSize = 735;
        _melSpectrogram = new(_sampleRate, (int)Math.Pow(2, (int)MathF.Log(_bufferSize,2)), frameSize, frameSize, _nMels, scaleType: MelSpecScaleType.NormalizedDecibel, reductionType: MelSpecReductionType.TimeMean);
        Debug.Log(_melSpectrogram);

        _embeddingImageModel = new FEPImageEmbedding(_textureTensor.Size[1], _textureTensor.Size[2], _textureTensor.Size[3]);
        int embedSize = _embeddingImageModel.GetOutputSize();
        _embeddingAudioModel = new FEPAudioEmbedding(_nMels, embedSize);
        _actorModel = new FEPActor(embedSize, _actionInterface.ActionCount);
        _embeddingOptimizer = new Adadelta(_learningRate, 0.001f, _embeddingImageModel.GetParameters());
        _actorOptimizer = new Adadelta(_learningRate, 0.001f, _actorModel.GetParameters());

        _globalEmbeddingImageModel = new FEPGlobalImageEmbedding(_textureTensor.Size[1], _globalBatchSize, _textureTensor.Size[2], _textureTensor.Size[3]);
        _globalEmbeddingAudioModel = new FEPGlobalAudioEmbedding(_nMels, _globalBatchSize, embedSize);
        _globalActorModel = new FEPGlobalActor(embedSize, _actionInterface.ActionCount);
        _globalEmbeddingOptimizer = new Adadelta(_learningRate, 0.001f, _globalEmbeddingImageModel.GetParameters());
        _globalActorOptimizer = new Adadelta(_learningRate, 0.001f, _globalActorModel.GetParameters());
        
        _mseLoss = new MSELoss();
        _actionLearnMask = new(_actionInterface.GetLearnMask(), new int[]{1,_actionInterface.ActionCount});
        //_actionGlobalMaskBuffer = new Tensor[_globalBatchSize];
        //var dummyGlobalMask = new Tensor(_actionInterface.GetGlobalMask(), new int[]{_actionInterface.ActionCount});
        //Array.Fill(_actionGlobalMaskBuffer,dummyGlobalMask);

        _globalAudioBuffer = new Tensor[_globalBatchSize];
        _globalImageBuffer = new Tensor[_globalBatchSize];
        _globalBufferIdx = 0;
        Debug.Log("FEP Agent Initialized.");
        _isReady = true;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        int dlen = data.Length;
        if (dlen <= 0) return;
        if (channels > 1)
        {
            int clen = dlen / channels;
            int offset;
            int doffset = 0;
            if (clen >= _bufferSize)
            {
                offset = 0;
                doffset = clen - _bufferSize;
            }
            else
            {
                Array.Copy(_audioBuffer, clen, _audioBuffer, 0, _bufferSize - clen);
                offset = _bufferSize - clen;
            }
            for(int i = doffset*channels; i < dlen; i += channels)
            {
                float v = 0f;
                for(int j = i; j < i+channels; j++)
                {
                    v += data[j];
                }
                _audioBuffer[i / channels + offset] = v / channels;
            }
        }else{
            if (dlen >= _bufferSize)
            {
                Array.Copy(data, _bufferSize - dlen, _audioBuffer, 0, dlen);
            }else{
                Array.Copy(_audioBuffer, dlen, _audioBuffer, 0, _bufferSize - dlen);
                Array.Copy(data, 0, _audioBuffer, _bufferSize - dlen, dlen);
            }
        }
        if (_useMicrophone)
        {
            Array.Fill(data,0f);
        }
    }

    IEnumerator ActionLoop()
    {
        yield return new WaitForEndOfFrame();
        TensorBox currentEmbeddedImageState;
        TensorBox currentEmbeddedAudioState;
        while (_isRunning)
        {
            while ((!_isReady) || _paused || (!_audioSource.isPlaying) || (_useWebCam && !_webCamRenderer.IsReady))
            {
                yield return new WaitForEndOfFrame();
            }
            _inferencing = true;
            Tensor currentImage = CaptureObservation();
            currentEmbeddedImageState = _embeddingImageModel.Forward(currentImage);
            _audioBufferTensor.SetData(_audioBuffer);
            var currentAudio = Tensor.Unsqueeze(_melSpectrogram.FromAudio(_audioBufferTensor),0);
            currentEmbeddedAudioState = _embeddingAudioModel.Forward(currentAudio);
            
            Tensor gMask = null;
            if (_lastEmbeddedImageState is not null && _lastLogProb is not null && (_currentFrames >= _updateIntervalFrames))
            {
                _currentFrames = 0;
                //Debug.Log($"LES: {_lastEmbeddedState}, CES: {currentEmbeddedState}");
                var distanceSq = -(_mseLoss.Forward(_lastEmbeddedImageState, currentEmbeddedAudioState.Detach()).Mean() + 2f).Log();
                distanceSq += -(_mseLoss.Forward(_lastEmbeddedAudioState, currentEmbeddedImageState.Detach()).Mean() + 2f).Log();
                // --- Actor Update ---
                float instantReward = distanceSq.GetTensor().Item();
                
                // Update Moving Average
                var reward = instantReward - _movingAvgReward;
                var globalMask = _actionInterface.GetGlobalMask();
                gMask = new Tensor(globalMask, new int[]{1, globalMask.Length});
                var localInverseMask = 1f - gMask;
                
                var actorLoss = (-(_lastLogProb * reward) * (_actionLearnMask * localInverseMask)).Sum();
                
                if (_movingAvgReward == 0) _movingAvgReward = instantReward;
                else _movingAvgReward = (1 - _rewardMovingAvgFactor) * _movingAvgReward + _rewardMovingAvgFactor * instantReward;
                
                Debug.Log($"[Local] LogProb: {_lastLogProb}, Reward: {reward}, Loss: {actorLoss}");
                _embeddingOptimizer.ZeroGrad();
                (-distanceSq).Backward();
                _embeddingOptimizer.Step();
                _actorOptimizer.ZeroGrad();
                actorLoss.Backward();
                _actorOptimizer.Step();

                currentEmbeddedImageState = _embeddingImageModel.Forward(currentImage);
                currentEmbeddedAudioState = _embeddingAudioModel.Forward(currentAudio);
            }
            if (_globalBufferIdx < _globalBatchSize)
            {
                _globalAudioBuffer[_globalBufferIdx] = currentAudio;
                _globalImageBuffer[_globalBufferIdx] = currentImage;
                _globalBufferIdx++;
            }
            else
            {
                _globalBufferIdx = 0;
                var audioBatch = Tensor.Stack(2,_globalAudioBuffer); 
                var imageBatch = Tensor.Stack(2,_globalImageBuffer);

                var currentGlobalEmbeddedImage = _globalEmbeddingImageModel.Forward(imageBatch);
                var currentGlobalEmbeddedAudio = _globalEmbeddingAudioModel.Forward(audioBatch);

                if (_lastGlobalEmbeddedImageState is not null && _lastGlobalLogProb is not null){
                    var distanceSq = -(_mseLoss.Forward(_lastGlobalEmbeddedImageState, currentGlobalEmbeddedAudio.Detach()).Mean() + 2f).Log();
                    distanceSq += -(_mseLoss.Forward(_lastGlobalEmbeddedAudioState, currentGlobalEmbeddedImage.Detach()).Mean() + 2f).Log();
                    if (gMask is null)
                    {
                        var globalMask = _actionInterface.GetGlobalMask();
                        gMask = new Tensor(globalMask, new int[]{1, globalMask.Length});
                    }
                    var actorLoss = (-(_lastGlobalLogProb * _movingAvgReward) * (_actionLearnMask * gMask)).Sum();

                    Debug.Log($"[Global] LogProb: {_lastGlobalLogProb}, Reward: {_movingAvgReward}, Loss: {actorLoss}");

                    _globalEmbeddingOptimizer.ZeroGrad();
                    (-distanceSq).Backward();
                    _globalEmbeddingOptimizer.Step();
                    _globalActorOptimizer.ZeroGrad();
                    actorLoss.Backward();
                    _globalActorOptimizer.Step();

                    currentGlobalEmbeddedImage = _globalEmbeddingImageModel.Forward(imageBatch);
                    currentGlobalEmbeddedAudio = _globalEmbeddingAudioModel.Forward(audioBatch);
                }
                var gDist = _globalActorModel.Forward(currentEmbeddedAudioState.Detach());
                var gActionTensor = gDist.Mode.Detach();
                var gLogProb = gDist.LogProb(gActionTensor);
                _actionInterface.SetGlobalParameters(gActionTensor.GetTensor().GetContiguousData(), gLogProb.GetTensor().GetContiguousData());
                _lastGlobalEmbeddedAudioState = currentGlobalEmbeddedAudio;
                _lastGlobalEmbeddedImageState = currentGlobalEmbeddedImage;
                _lastGlobalLogProb = gLogProb;
            }
            //Debug.Log($"LES: {_lastEmbeddedState}, CES: {currentEmbeddedState}");
            var dist = _actorModel.Forward(currentEmbeddedAudioState.Detach());
            var actions = dist.Mode.Detach();
            var logProb = dist.LogProb(actions);
            _lastLogProbData = logProb.GetTensor().GetContiguousData();
            _lastActionsData = actions.GetTensor().GetContiguousData();
            _actionInterface.Execute(_lastActionsData, _lastLogProbData);
            _actionLearnMask.SetData(_actionInterface.GetLearnMask());
            //_actionGlobalMaskBuffer[_globalBufferIdx].SetData(_actionInterface.GetGlobalMask());
            
            _lastEmbeddedImageState = currentEmbeddedImageState;
            _lastEmbeddedAudioState = currentEmbeddedAudioState;
            _lastLogProb = logProb;
            _currentFrames++;
            _inferencing = false;
            onAIUpdated?.Invoke();
            yield return new WaitForEndOfFrame();
        }
    }
    private Tensor CaptureObservation()
    {
        if (_useWebCam){
            _webCamRenderer.Render(_texture2D);
        }
        else
        {
            var prevTarget = _camera.targetTexture;
            _camera.targetTexture = _renderTexture;
            _camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _renderTexture;

            _texture2D.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
            _texture2D.Apply();
            RenderTexture.active = previous;
            _camera.targetTexture = prevTarget;
        }
        _textureTensor.SetAsConicalHSV(_texture2D);
        return _textureTensor;
    }
    void OnDestroy()
    {
        // 1. ループを止める
        _isRunning = false;
        StopAllCoroutines();

        // 2. マイクの解放 (これをしないと次回起動時に録音できないことがある)
        if (_useMicrophone && !string.IsNullOrEmpty(_activatedMicrophone))
        {
            if (Microphone.IsRecording(_activatedMicrophone))
            {
                Microphone.End(_activatedMicrophone);
            }
        }

        // 3. AudioSourceの停止
        if (_audioSource != null)
        {
            _audioSource.Stop();
            _audioSource.clip = null;
        }

        // 4. WebCamの停止
        if (_webCamRenderer != null)
        {
            _webCamRenderer.Stop();
        }

        // 5. 動的に作ったTexture2Dの破棄 (重要: これをやらないとVRAMリークする)
        if (_texture2D != null)
        {
            Destroy(_texture2D);
            _texture2D = null;
        }

        // 6. 巨大なバッファの参照を切ってGCを助ける
        _audioBuffer = null;
        _globalAudioBuffer = null;
        _globalImageBuffer = null;
        //_actionGlobalMaskBuffer = null;
        
        // Tensor類の参照も切っておく（SimpleNNがNativeメモリを使っていないならGC任せでOK）
        _textureTensor = null;
        _melSpectrogram = null;
        
        Debug.Log("[FEPAgent] Resources cleaned up.");
    }
}
