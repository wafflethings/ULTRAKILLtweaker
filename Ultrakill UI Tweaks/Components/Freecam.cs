using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ULTRAKILLtweaker
{
    /*
     *  This code is not written by me.
     * 
     *  Copyright 2019 Ashley Davis
     *  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
     *  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
     *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
     */

    public class Freecam : MonoBehaviour
    {
        // Normal speed of camera movement.
        public float MovementSpeed = 10f;

        // Speed of camera movement when shift is held down,
        public float FastMovementSpeed = 100f;

        // Sensitivity for free look.
        public float FreeLookSensitivity = 3f;

        // Amount to zoom the camera when using the mouse wheel.
        public float ZoomSensitivity = 10f;

        // Amount to zoom the camera when using the mouse wheel (fast mode).
        public float FastZoomSensitivity = 50f;

        // Set to true when free looking (on right mouse button).
        private bool Looking = false;

        public void Update()
        {
            var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var movementSpeed = fastMode ? FastMovementSpeed : MovementSpeed;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                transform.position = transform.position + (-transform.right * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                transform.position = transform.position + (transform.right * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                transform.position = transform.position + (transform.forward * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                transform.position = transform.position + (-transform.forward * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.Q))
            {
                transform.position = transform.position + (transform.up * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.E))
            {
                transform.position = transform.position + (-transform.up * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
            {
                transform.position = transform.position + (Vector3.up * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
            {
                transform.position = transform.position + (-Vector3.up * movementSpeed * Time.deltaTime);
            }

            if (Looking)
            {
                float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * FreeLookSensitivity;
                float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * FreeLookSensitivity;
                transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            }

            float axis = Input.GetAxis("Mouse ScrollWheel");
            if (axis != 0)
            {
                var zoomSensitivity = fastMode ? FastZoomSensitivity : ZoomSensitivity;
                transform.position = transform.position + transform.forward * axis * zoomSensitivity;
            }

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                StartLooking();
            }
            else if (Input.GetKeyUp(KeyCode.Mouse1))
            {
                StopLooking();
            }
        }

        void OnDisable()
        {
            StopLooking();
        }

        public void StartLooking()
        {
            Looking = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void StopLooking()
        {
            Looking = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
