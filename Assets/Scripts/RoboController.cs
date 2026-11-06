using UnityEngine;

// Goes on the Robot parent object. Everything parented to Robot moves with it.
public class RoboController : MonoBehaviour
{
    public float Speed = 5.2f;      // Walking speed in units per second
    public float TurnSpeed = 60f;   // Turning speed in degrees per second
    public bool Manual = false;     // False: walks by itself. True: the arrow keys drive it.
    public bool IsWalking = false;  // The limbs read this to know when to swing

    void Update()
    {
        // GetKeyDown is true only on the frame the key goes down,
        // so one press switches modes once. The ! flips true and false.
        if (Input.GetKeyDown(KeyCode.Space)) Manual = !Manual;

        if (Manual)
        {
            // Stay still unless a movement key below is held.
            IsWalking = false;

            if (Input.GetKey(KeyCode.UpArrow))
            {
                transform.Translate(0, 0, Speed * Time.deltaTime);
                IsWalking = true;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                transform.Translate(0, 0, -Speed * Time.deltaTime);
                IsWalking = true;
            }

            if (Input.GetKey(KeyCode.RightArrow)) transform.Rotate(0, TurnSpeed * Time.deltaTime, 0);
            if (Input.GetKey(KeyCode.LeftArrow)) transform.Rotate(0, -TurnSpeed * Time.deltaTime, 0);
        }
        else
        {
            // Automatic mode: always walk forward.
            transform.Translate(0, 0, Speed * Time.deltaTime);
            IsWalking = true;
        }
    }
}
