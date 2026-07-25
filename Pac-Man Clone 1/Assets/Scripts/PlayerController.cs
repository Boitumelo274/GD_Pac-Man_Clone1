using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 4.0f;

    private Vector2 direction = Vector2.zero;
    void Start()
    {
        
    }

    void Update()
    {
        CheckInput();

        Move();

        UpdateRotation();
       
    }

   void CheckInput()
   {
        if (Input.GetKeyDown(KeyCode.LeftArrow)){

            direction = Vector2.left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow)){
            
            direction = Vector2.right;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow)){

            direction = Vector2.up;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow)){

            direction = Vector2.down;
        }
   }

    void Move ()
    {
        transform.localPosition += (Vector3)(direction * speed) * Time.deltaTime;
    }


    void UpdateRotation()
    {
        if (direction == Vector2.left) {

            transform.localScale = new Vector3(-0.5f, 0.5f, 0.5f);
            transform.localRotation = Quaternion.Euler(0, 0, 0);

        } else if (direction == Vector2.right) { 

            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); ;
            transform.localRotation = Quaternion.Euler(0, 0,0);

        }
        else if (direction ==Vector2.up) {

            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); ;
            transform.localRotation = Quaternion.Euler(0, 0, 90);

        }else if (direction ==  Vector2.down) {

            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); ;
            transform.localRotation = Quaternion.Euler(0, 0, 270);

        }
    }
}