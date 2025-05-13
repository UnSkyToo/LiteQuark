using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformShakingAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformShaking>({TS.name},{_totalTime},{_strength},{_vibrato},{_randomness})";

        private readonly float _totalTime;
        private readonly float _perStepTime;
        private readonly float _strength;
        private readonly int _vibrato;
        private readonly float _randomness;

        private float _degree;
        private Vector3 _position;
        private float _currentTime;
        private float _stepTime;

        public TransformShakingAction(Transform transform, float time, float strength, int vibrato, float randomness = 90.0f)
            : base(transform)
        {
            _position = TS.localPosition;
            _totalTime = MathUtils.ClampMinTime(time);
            _perStepTime = 1.0f / vibrato;
            _strength = strength;
            _vibrato = vibrato;
            _randomness = randomness;
        }
        
        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            _position = TS.localPosition;
            _currentTime = 0;
            _stepTime = 0;
            _degree = Random.Range(0f, 360f);
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            if (!CheckSafety())
            {
                return;
            }
            
            _currentTime += deltaTime;
            _stepTime += deltaTime;

            if (_stepTime >= _perStepTime)
            {
                Shake();
                _stepTime -= _perStepTime;
            }

            if (_currentTime >= _totalTime)
            {
                TS.localPosition = _position;
                IsEnd = true;
            }
        }

        private void Shake()
        {
            _degree = _degree - 180f + Random.Range(-_randomness, _randomness);
            
            var quaternion = Quaternion.AngleAxis(Random.Range(-_randomness, _randomness), Vector3.up);
            TS.localPosition = _position + quaternion * MathUtils.Vector3FromAngle(_degree, _strength);
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder TransformShaking(this ActionBuilder builder, Transform transform, float time, float strength, int vibrato, float randomness = 90.0f)
        {
            builder.Add(new TransformShakingAction(transform, time, strength, vibrato, randomness));
            return builder;
        }
    }
}