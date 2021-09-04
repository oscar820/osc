using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.QFixed;
namespace QTool.QFixed
{ 
    public enum Space
    {
        World = 0,
        Self = 1
    }
    [ExecuteInEditMode]
    public class FixedTransform : MonoBehaviour
    {
        [SerializeField,HideInInspector]
        private Fixed3 _localPosition;

        public Fixed3 localPosition {
            get => _localPosition;set => _localPosition = value;
        }

        [SerializeField]
        private Fixed3 _position;

        private Fixed3 _prevPosition;
        public Fixed3 position
        {
            get
            {
                if (transform.parent != null)
                {
                    _position = transform.position.ToFixed3();
                  
                }
                return _position;
            }
            set
            {
                if (value == _position) return;
                _position = value;
                OnTransformChange?.Invoke();
                transform.position = _position.ToVector3();
                UpdateChildPosition();
            }
        }
        [SerializeField, HideInInspector]
        private FixedQuaternion _localRotation = FixedQuaternion.identity;
        public System.Action OnTransformChange;
        public FixedQuaternion localRotation
        {
            get
            {
                return _localRotation;
            }
            set
            {
                _localRotation = value;
            }
        }

        [SerializeField]
        private FixedQuaternion _rotation = FixedQuaternion.identity;
        public FixedQuaternion rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                if (value == _rotation) return;
                _rotation = value;
                OnTransformChange?.Invoke();
                UpdateChildRotation();
            }
        }
        [SerializeField, HideInInspector]
        private Fixed3 _scale;
        public Fixed3 scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (value == _scale) return;
                OnTransformChange?.Invoke();
                _scale = value;
            }
        }
        [SerializeField, HideInInspector]
        private Fixed3 _localScale;
        public Fixed3 localScale
        {
            get
            {
                return _localScale;
            }
            set
            {
                _localScale = value;
            }
        }



        [HideInInspector]
        public FixedTransform fixedParent;
        public FixedMatrix4x4 localToWorldMatrix
        {
            get
            {
                FixedTransform thisTransform = this;
                FixedMatrix4x4 curMatrix = FixedMatrix4x4.TransformToMatrix(ref thisTransform);
                FixedTransform parent = fixedParent;
                while (parent != null)
                {
                    curMatrix = FixedMatrix4x4.TransformToMatrix(ref parent) * curMatrix;
                    parent = parent.fixedParent;
                }
                return curMatrix;
            }
        }

        public FixedMatrix4x4 worldToLocalMatrix
        {
            get
            {
                return FixedMatrix4x4.Inverse(localToWorldMatrix);
            }
        }
        [HideInInspector]
        public List<FixedTransform> fixedChildren;

   
        public void LookAt(Fixed3 target)
        {
            this.rotation = FixedQuaternion.CreateFromMatrix(FixedMatrix3x3.CreateFromLookAt(position, target));
        }
        public void RotateAround(Fixed3 point, Fixed3 axis, Fixed angle)
        {
            Fixed3 vector = this.position;
            Fixed3 vector2 = vector - point;
            vector2 = Fixed3.Transform(vector2, FixedMatrix3x3.AngleAxis(angle * MathFixed.PI2Rad, axis));
            vector = point + vector2;
            this.position = vector;
            Rotate(axis, angle);
        }
        public void RotateAround(Fixed3 axis, Fixed angle)
        {
            Rotate(axis, angle);
        }
        public void Rotate(Fixed3 axis, Fixed angle, Space relativeTo= Space.Self)
        {
            FixedQuaternion result = FixedQuaternion.identity;

            if (relativeTo == Space.Self)
            {
                result = this.rotation * FixedQuaternion.AngleAxis(angle, axis);
            }
            else
            {
                result = FixedQuaternion.AngleAxis(angle, axis) * this.rotation;
            }
            result.Normalize();
            this.rotation = result;
        }

        public void Rotate(Fixed3 eulerAngles, Space relativeTo= Space.Self)
        {
            FixedQuaternion result = FixedQuaternion.identity;

            if (relativeTo == Space.Self)
            {
                result = this.rotation * FixedQuaternion.Euler(eulerAngles);
            }
            else
            {
                result = FixedQuaternion.Euler(eulerAngles) * this.rotation;
            }
            result.Normalize();
            this.rotation = result;
        }
        public Fixed3 forward => Fixed3.Transform(Fixed3.forward, FixedMatrix3x3.CreateFromQuaternion(rotation));
        public Fixed3 right => Fixed3.Transform(Fixed3.right, FixedMatrix3x3.CreateFromQuaternion(rotation));
        public Fixed3 up => Fixed3.Transform(Fixed3.up, FixedMatrix3x3.CreateFromQuaternion(rotation));
        public Fixed4 TransformPoint(Fixed4 point)
        {
            Debug.Assert(point.w == Fixed.one);
            return Fixed4.Transform(point, localToWorldMatrix);
        }
        public Fixed3 TransformPoint(Fixed3 point)
        {
            return Fixed4.Transform(point, localToWorldMatrix).ToFixed3();
        }
        public Fixed4 InverseTransformPoint(Fixed4 point)
        {
            Debug.Assert(point.w == Fixed.one);
            return Fixed4.Transform(point, worldToLocalMatrix);
        }

        public Fixed3 InverseTransformPoint(Fixed3 point)
        {
            return Fixed4.Transform(point, worldToLocalMatrix).ToFixed3();
        }
        public Fixed4 TransformDirection(Fixed4 direction)
        {
            Debug.Assert(direction.w == Fixed.zero);
            FixedMatrix4x4 matrix = FixedMatrix4x4.Translate(position) * FixedMatrix4x4.Rotate(rotation);
            return Fixed4.Transform(direction, matrix);
        }

        public Fixed3 TransformDirection(Fixed3 direction)
        {
            return TransformDirection(new Fixed4(direction.x, direction.y, direction.z, Fixed.zero)).ToFixed3();
        }

        public Fixed4 InverseTransformDirection(Fixed4 direction)
        {
            Debug.Assert(direction.w == Fixed.zero);
            FixedMatrix4x4 matrix = FixedMatrix4x4.Translate(position) * FixedMatrix4x4.Rotate(rotation);
            return Fixed4.Transform(direction, FixedMatrix4x4.Inverse(matrix));
        }

        public Fixed3 InverseTransformDirection(Fixed3 direction)
        {
            return InverseTransformDirection(new Fixed4(direction.x, direction.y, direction.z, Fixed.zero)).ToFixed3();
        }
        public Fixed4 TransformVector(Fixed4 vector)
        {
            Debug.Assert(vector.w == Fixed.zero);
            return Fixed4.Transform(vector, localToWorldMatrix);
        }

        public Fixed3 TransformVector(Fixed3 vector)
        {
            return TransformVector(new Fixed4(vector.x, vector.y, vector.z, Fixed.zero)).ToFixed3();
        }
        public Fixed4 InverseTransformVector(Fixed4 vector)
        {
            Debug.Assert(vector.w == Fixed.zero);
            return Fixed4.Transform(vector, worldToLocalMatrix);
        }

        public Fixed3 InverseTransformVector(Fixed3 vector)
        {
            return InverseTransformVector(new Fixed4(vector.x, vector.y, vector.z, Fixed.zero)).ToFixed3();
        }

        void UpdateChildRotation()
        {
            FixedMatrix3x3 matrix = FixedMatrix3x3.CreateFromQuaternion(_rotation);
            foreach (FixedTransform child in fixedChildren)
            {
                child.localRotation = FixedQuaternion.CreateFromMatrix(FixedMatrix3x3.Inverse(matrix)) * _rotation;
                child.localPosition = Fixed3.Transform(child.localPosition, FixedMatrix3x3.CreateFromQuaternion(child.localRotation));
                child.position =TransformPoint(child.localPosition);
            }
        }
        public void Translate(Fixed3 translation, Space relativeTo= Space.Self)
        {
            if (relativeTo == Space.Self)
            {
                Translate(translation, this);
            }
            else
            {
                this.position += translation;
            }
        }
        public void Translate(Fixed3 translation, FixedTransform relativeTo)
        {
            this.position += Fixed3.Transform(translation, FixedMatrix3x3.CreateFromQuaternion(relativeTo.rotation));
        }
        private void UpdateChildPosition()
        {
            foreach (FixedTransform child in fixedChildren)
            {
                child.Translate(_position - _prevPosition);
            }
        }
        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            foreach (Transform child in transform)
            {
                FixedTransform tsChild = child.GetComponent<FixedTransform>();
                if (tsChild != null)
                {
                    fixedChildren.Add(tsChild);
                }
            }
        }
        private void Update()
        {
            if (Application.isPlaying)
            {
                UpdatePlayMode();
            }
            else
            {
                UpdateEditMode();
            }
        }

        private void UpdateEditMode()
        {
            if (transform.hasChanged)
            {
                position = transform.position.ToFixed3();
                rotation = transform.rotation.ToFixedQuaternion();
                scale = transform.lossyScale.ToFixed3();

                _localPosition = transform.localPosition.ToFixed3();
                _localRotation = transform.localRotation.ToFixedQuaternion();
                _localScale = transform.localScale.ToFixed3();
            }
        }
        private void UpdatePlayMode()
        {
            if (fixedParent != null)
            {
                _localPosition = fixedParent.InverseTransformPoint(position);
                FixedMatrix3x3 matrix = FixedMatrix3x3.CreateFromQuaternion(fixedParent.rotation);
                _localRotation = FixedQuaternion.CreateFromMatrix(FixedMatrix3x3.Inverse(matrix)) * rotation;
            }
            else
            {
                _localPosition = position;
                _localRotation = rotation;
            }
            transform.position = position.ToVector3();
            transform.rotation = rotation.ToQuaternion();
            transform.localScale = localScale.ToVector3();
            _scale = transform.lossyScale.ToFixed3();
        }
    }
}
