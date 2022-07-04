using System;
using System.ComponentModel;
using UnityEngine;
public class EnemyController : MonoBehaviour
    {
        private bool isActive;
        private bool canDestroy;
        private float restTime, activeTime, movementSpeed;
        public GameObject playerObj;
        public MovementTypeSO movement;
        
        private float walkUntil, waitUntil;
        private bool isWalking;
        private bool hasMoveDirBeenChosen, dataHasBeenLoaded;
        private float lastX, lastY;
        private Vector3 targetMoveDir;
        private Rigidbody2D enemyRigidBody;

        private bool _hasGotComponents;

        private void Start()
        {
            if (!_hasGotComponents)
            {
                GetAllComponents();
            }
        }

        private void GetAllComponents()
        {
            enemyRigidBody = gameObject.GetComponent<Rigidbody2D>();
            _hasGotComponents = true;
        }

        private void Awake()
        {
            isActive = true;
            canDestroy = false;
            dataHasBeenLoaded = false;
            _hasGotComponents = false;
        }

        private void FixedUpdate()
        {
            if (!dataHasBeenLoaded || !isActive || canDestroy) return;
            if (isWalking)
            {
                if (walkUntil > 0)
                    Walk();
                else
                {
                    walkUntil = 0.0f; // ué?
                    isWalking = false;
                    waitUntil = restTime;
                    hasMoveDirBeenChosen = false;
                }
            }
            else
            {
                if (waitUntil > 0f)
                    Wait();
                else
                {
                    waitUntil = 0;
                    isWalking = true;
                    walkUntil = activeTime;
                }
            }
        }

        private void Walk()
        {
            if (!hasMoveDirBeenChosen)
            {
                int xOffset, yOffset;
                targetMoveDir = movement.MovementType(playerObj.transform.position, gameObject.transform.position);
                targetMoveDir.Normalize();
                
                if (targetMoveDir.x > 0)
                    xOffset = 1;
                else if (targetMoveDir.x < 0)
                    xOffset = -1;
                else
                    xOffset = 0;
                if (targetMoveDir.y > 0)
                    yOffset = 1;
                else if (targetMoveDir.y < 0) 
                    yOffset = -1;
                else
                    yOffset = 0;
                targetMoveDir = new Vector3((targetMoveDir.x), (targetMoveDir.y), 0f);

                hasMoveDirBeenChosen = true;
            }
            transform.position += new Vector3(targetMoveDir.x * movementSpeed * Time.fixedDeltaTime, targetMoveDir.y * movementSpeed * Time.fixedDeltaTime, 0f);
            walkUntil -= Time.deltaTime;
        }

        private void Wait()
        {
            enemyRigidBody.velocity = Vector3.zero;
            waitUntil -= Time.deltaTime;
        }

        
        public void LoadEnemyData(EnemySO enemyData)
        {
            if (!_hasGotComponents)
            {
                GetAllComponents();
            }
            movementSpeed = enemyData.movementSpeed;
            restTime = enemyData.restTime;
            activeTime = enemyData.activeTime;

            hasMoveDirBeenChosen = false;
            
            isWalking = false;
            waitUntil = 0.5f;
            dataHasBeenLoaded = true;
        }
    }