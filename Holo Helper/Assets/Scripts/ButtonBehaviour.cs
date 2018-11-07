﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using UnityEngine.XR.WSA.Input;
using HoloToolkit.UI.Keyboard;
using System.IO;

public class ButtonBehaviour : MonoBehaviour, IInputClickHandler, IFocusable {

    // Other objects
    public GameObject actMan;                       // activity manager gameObject
    private GameObject storedActs;                  // Stored Activities gameObject
    private GameObject storedInstruction;           // Stored Instructions gameObject
    private GameObject[] menus = new GameObject[5]; // array containing menus
    private Material[] materials = new Material[2]; // array containing materials
    private ActivityManager ams;                    // script of activity manager

    // Page info
    private int visibleActs = 5;                    // number of items per page (basically locked to 5)
    private Vector3[] activityPos;                  // array of button positions

    private GameObject obj;                         // used when creating buttons
    private GameObject gazedAtObj;                  // currently gazed at object

    public Instructions connectedInstruction;
    public Activity connectedAct;

    // bools that set this button's status
    // determines which actions it can perform depending on current menu
    public bool isAdmin = false;
    public bool isActivity = false;
    public bool isInstruction = false;
    public bool isCreate = false;
    public bool isEdit = false;
    public bool isChangeActivityName = false;
    public bool isDelete = false;
    public bool isReturn = false;
    public bool isPageLeft = false;
    public bool isPageRight = false;

    bool isEditActivity = false;
    bool isEditInstruction = false;

    int keyboardCase = -1;                          // variable for switch/case
    bool isKeyboard = false;                        // determines if keyboard is active
    Keyboard keyboard;                              // the keyboard
    string keyboardText = "";                       // the text of the keyboard

    /* ------------------------------------ */
    /* General Functions */
    /* ------------------------------------ */

    // Use this for initialization
    void Start ()
    {
        ams = actMan.GetComponent<ActivityManager>();

        menus = ams.menus;
        materials = ams.materials;
        storedActs = ams.storedAct;
        storedInstruction = ams.storedIns;
        activityPos = ams.GetActivityPos();

        keyboard = Keyboard.Instance;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if(this.gameObject == ams.GetSelectedObject())
        {
            ams.GetSelectedObject().GetComponent<Renderer>().material = materials[1];
        }

        if (isKeyboard)
        {
            if (!keyboard.isActiveAndEnabled)
            {
                switch (keyboardCase)
                {
                    case 1: // when creating activity
                        InstantiateActivityButton(keyboardText, null);
                        menus[1].transform.GetChild(0).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
                        menus[2].transform.GetChild(0).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
                        break;

                    case 2: // when creating instruction
                        InstantiateInstructionButton(keyboardText, null);
                        menus[3].transform.GetChild(1).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
                        //menus[2].transform.GetChild(1).GetComponent<TextMesh>().text = "Page " + (ams.currentPageInstruction + 1) + " / " + (ams.noOfPagesInstruction + 1);
                        break;

                    case 3: // when editing instructions
                        ams.SetNameInstruction(keyboardText);
                        ams.GetSelectedInstruction().indicator.SetActive(true);
                        menus[3].SetActive(false);
                        storedInstruction.SetActive(false);
                        GameObject.Find("INDICATORPLACEMENTMENU").transform.GetChild(0).gameObject.SetActive(true);
                        GameObject.Find("INDICATORPLACEMENTMENU").transform.GetChild(1).gameObject.SetActive(true);
                        GameObject.Find("INDICATORPLACEMENTMENU").transform.GetChild(2).gameObject.SetActive(true);
                        break;

                    case 4: // when change activity name
                       // ams.SetSelectedObject(null);
                        actMan.GetComponent<ActivityManager>().SetNameActivity(keyboardText);
                        menus[3].transform.GetChild(0).GetComponent<TextMesh>().text = keyboardText;
                        for(int i = 2; i < storedActs.transform.childCount; i++)
                        {
                            if(storedActs.transform.GetChild(i).GetComponent<ButtonBehaviour>().connectedAct.name == keyboardText)
                            {

                                storedActs.transform.GetChild(i).name = keyboardText;

                                if (keyboardText.Length > 20)
                                {
                                    storedActs.transform.GetChild(i).GetChild(0).GetComponent<TextMesh>().text = keyboardText.Remove(20) + "...";
                                }
                                else
                                {
                                    storedActs.transform.GetChild(i).GetChild(0).GetComponent<TextMesh>().text = keyboardText;
                                }
                                break;
                            }
                        }
                        break;

                    default:
                        break;
                }

                // prevent potential misfire of switch/case
                keyboardCase = -1;
                
                keyboardText = "";
                isKeyboard = false;
            }

            keyboardText = keyboard.InputField.text;
        }
    }

    /* ------------------------------------ */
    /* Hololens Functions */
    /* ------------------------------------ */

    public void OnInputClicked(InputClickedEventData eventData)
    {
        // Unhighlight the highlighted button
        this.gameObject.GetComponent<Renderer>().material = materials[0];

        // 0: Main Menu
        if (menus[0].activeSelf)
        {
            MainMenu(eventData);
            storedActs.SetActive(true);
        }

        // 1: Administrator Menu
        else if (menus[1].activeSelf)
        {
            AdminMenu(eventData);
        }

        // ---------------------------------------------

        // 2: User Menu
        else if (menus[2].activeSelf)
        {
            UserMenu(eventData);
        }

        // ---------------------------------------------

        // 3: Edit Menu
        else if (menus[3].activeSelf)
        {
            ActivityEditMenu(eventData);
            
        }

        // ---------------------------------------------

        // 4: Activity Menu
        else if (menus[4].activeSelf)
        {
            ActivityEventMenu(eventData);
        }
    }

    public void OnFocusEnter()
    {
        gazedAtObj = this.gameObject;

        // Highlight looked at object
        this.gameObject.GetComponent<Renderer>().material = materials[1];
    }

    public void OnFocusExit()
    {
        gazedAtObj = null;

        // Unhighlight looked at object, so long as it's not the selected object
        if (this.gameObject != actMan.GetComponent<ActivityManager>().GetSelectedObject())
        {
            this.gameObject.GetComponent<Renderer>().material = materials[0];
        }
    }
    
    /* ------------------------------------ */
    /* Menu Functions */
    /* ------------------------------------ */

    public void MainMenu(InputClickedEventData eventData)
    {
        if (ams.GetFirstTime())
        {
            foreach (Activity a in ams.container.activities)
            {
                InstantiateActivityButton(a.name, a);
            }

            ams.SetFirstTime(false);
        }

        if (isAdmin)
        {
            ams.SetCurrentPage(0);
            ams.ChangePage(storedActs);
            menus[1].transform.GetChild(0).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
            menus[1].SetActive(true);
            menus[0].SetActive(false);
            menus[2].SetActive(false);
            menus[3].SetActive(false);
        }
        else
        {
            ams.SetCurrentPage(0);
            ams.ChangePage(storedActs);
            menus[2].SetActive(true);
            menus[0].SetActive(false);
            menus[1].SetActive(false);
            menus[3].SetActive(false);
        }
    }

    public void AdminMenu(InputClickedEventData eventData)
    {
        if (isCreate)
        {
            CreateKeyboard(1, null);
        }
        else if (isActivity)
        {
            if (ams.GetSelectedObject() != null)
            {
                ams.GetSelectedObject().GetComponent<Renderer>().material = materials[0];
            }

            ams.SetSelectedObject(gazedAtObj);
        }
        else if (isEdit)
        {
            //CreateKeyboard(false);
            storedActs.SetActive(false);
            storedInstruction.SetActive(true);
            menus[0].SetActive(false);
            menus[1].SetActive(false);
            menus[2].SetActive(false);
            menus[3].SetActive(true);
            ams.SetSelectedActivity(ams.container.activities.Find(x => x.name == ams.GetSelectedObject().name)); //
            menus[3].transform.GetChild(0).GetComponent<TextMesh>().text = ams.GetSelectedObject().name;
            foreach (Instructions i in ams.GetSelectedActivity().instructions)
            {
                InstantiateInstructionButton(i.instructionText, i);
            }

           
            ams.UpdatePageAmount(storedInstruction);
            ams.SetCurrentPage(0);
            ams.ChangePage(storedInstruction);
            menus[3].transform.GetChild(1).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
            //ams.ChangePageInstruction();

            //menus[1].SetActive(false);
        }
        else if (isDelete)
        {
            ams.DeleteActivity(ams.GetSelectedObject());
            menus[1].transform.GetChild(0).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
        }
        else if (isReturn)
        {
            if (Application.isEditor)
            {
                ams.container.Save(Path.Combine(Application.dataPath, "ActivityList.xml"));
            }
            else
            {
                ams.container.Save(Path.Combine(Application.persistentDataPath, "ActivityList.xml"));
            }

            storedActs.SetActive(false);
            menus[0].SetActive(true);
            menus[1].SetActive(false);
            menus[2].SetActive(false);
            
        }

        // Change page
        else if (isPageRight)
        {
            if (ams.GetCurrentPage() < ams.GetPageAmount())
            {
                ams.SetCurrentPage(ams.GetCurrentPage() + 1);
            }
            else
            {
                ams.SetCurrentPage(ams.GetPageAmount());
            }

            ams.ChangePage(storedActs);
            menus[1].transform.GetChild(0).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
        }
        else if (isPageLeft)
        {
            if (ams.GetCurrentPage() > 0)
            {
                ams.SetCurrentPage(ams.GetCurrentPage() - 1);
            }
            else
            {
                ams.SetCurrentPage(0);
            }

            ams.ChangePage(storedActs);
            menus[1].transform.GetChild(0).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
        }
    }

    public void UserMenu(InputClickedEventData eventData)
    {
        if (isActivity)
        {
            /*if (ams.GetSelectedObject() != null)
            {
                ams.GetSelectedObject().GetComponent<Renderer>().material = materials[0];
            }

            ams.SetSelectedObject(gazedAtObj);*/
            ams.SetSelectedActivity(gazedAtObj.GetComponent<ButtonBehaviour>().connectedAct);
            menus[4].transform.GetChild(1).GetComponent<TextMesh>().text = ams.GetSelectedActivity().name;
            ams.GetSelectedActivity().instructions[0].indicator.SetActive(true);
            storedActs.SetActive(false);
            menus[4].SetActive(true);
            menus[2].SetActive(false);
        }
        else if (isReturn)
        {
            storedActs.SetActive(false);
            menus[0].SetActive(true);
            menus[1].SetActive(false);
            menus[2].SetActive(false);
        }

        // Change page
        else if (isPageRight)
        {
            if (ams.GetCurrentPage() < ams.GetPageAmount())
            {
                ams.SetCurrentPage(ams.GetCurrentPage() + 1);
            }
            else
            {
                ams.SetCurrentPage(ams.GetPageAmount());
            }

            ams.ChangePage(storedActs);
            menus[2].transform.GetChild(0).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
        }
        else if (isPageLeft)
        {
            if (ams.GetCurrentPage() > 0)
            {
                ams.SetCurrentPage(ams.GetCurrentPage() - 1);
            }
            else
            {
                ams.SetCurrentPage(0);
            }

            ams.ChangePage(storedActs);
            menus[2].transform.GetChild(0).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
        }
    }

    public void ActivityEditMenu(InputClickedEventData eventData)
    {
        if (isCreate)
        {
            CreateKeyboard(2, null);
        }

        else if (isInstruction)
        {
            if (ams.GetSelectedObject() != null)
            {
                ams.GetSelectedObject().GetComponent<Renderer>().material = materials[1];
            }
            ams.SetSelectedObject(gazedAtObj);

        }

        else if (isEdit)
        {
            ams.SetSelectedInstruction(ams.GetSelectedActivity().instructions.Find(x => x.instructionText == ams.GetSelectedObject().name));
            CreateKeyboard(3, ams.GetSelectedObject().name);
            ams.GetSelectedObject().GetComponent<Renderer>().material = materials[0];
            
            //storedInstruction.SetActive(false);
        }

        else if (isDelete)
        {
            ams.DeleteInstruction(ams.GetSelectedObject());
            menus[3].transform.GetChild(1).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
        }

        else if(isChangeActivityName)
        {
            CreateKeyboard(4, null);
        }

        else if (isReturn)
        {
            storedActs.SetActive(true);
            storedInstruction.SetActive(false);
            menus[1].SetActive(true);
            menus[2].SetActive(false);
            menus[3].SetActive(false);
            ams.DeleteInstructionButton();
            ams.SetCurrentPage(0);
        }

        else if (isPageRight)
        {
            if (ams.GetCurrentPage() < ams.GetPageAmount())
            {
                ams.SetCurrentPage(ams.GetCurrentPage() + 1);
            }
            else
            {
                ams.SetCurrentPage(ams.GetPageAmount());
            }

            ams.ChangePage(storedInstruction);
            menus[3].transform.GetChild(1).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
        }
        else if (isPageLeft)
        {
            if (ams.GetCurrentPage() > 0)
            {
                ams.SetCurrentPage(ams.GetCurrentPage() - 1);
            }
            else
            {
                ams.SetCurrentPage(0);
            }

            ams.ChangePage(storedInstruction);
            menus[3].transform.GetChild(1).GetComponent<TextMesh>().text = "Page " + (ams.GetCurrentPage() + 1) + " / " + (ams.GetPageAmount() + 1);
        }
    }

    public void ActivityEventMenu(InputClickedEventData eventData)
    {
        if(isPageLeft && isPageRight)
        {
            ams.GetSelectedActivity().RepeatStep();
        }

        else if (isPageLeft)
        {
            ams.GetSelectedActivity().PreviousStep();
        }

        else if (isPageRight)
        {
            ams.GetSelectedActivity().NextStep();
        }

        else if (isReturn)
        {
            menus[2].SetActive(true);
            menus[4].SetActive(false);
            storedActs.SetActive(true);
        }
    }

    /* ------------------------------------ */
    /* Create Functions */
    /* ------------------------------------ */

    /** Create a new button with the correct initialization. */
    public void InstantiateActivityButton(string name, Activity act)
    {
        obj = ams.CreateActivity(name, act);
        obj.GetComponent<ButtonBehaviour>().storedActs = storedActs;
        obj.GetComponent<ButtonBehaviour>().menus = menus;
        obj.GetComponent<ButtonBehaviour>().isActivity = true;
        obj.transform.localScale = new Vector3(0.23f, 0.0234f, 0.02f);
        obj.transform.GetChild(0).localScale = new Vector3(0.07f, 0.7f, 1);
        obj.transform.SetParent(storedActs.transform);

        // position of menu based on amount of activities
        obj.transform.localPosition = activityPos[(ams.GetActivityAmount() - 1) % visibleActs];
    }

    /** Create a new button with the correct initialization. */
    public void InstantiateInstructionButton(string text, Instructions instruct)
    {
        obj = ams.CreateInstruction(text, instruct);
        obj.GetComponent<ButtonBehaviour>().storedInstruction = storedInstruction;
        obj.GetComponent<ButtonBehaviour>().menus = menus;
        obj.GetComponent<ButtonBehaviour>().isInstruction = true;
        obj.transform.localScale = new Vector3(0.23f, 0.0234f, 0.02f);
        obj.transform.GetChild(0).localScale = new Vector3(0.07f, 0.7f, 1);
        obj.transform.SetParent(storedInstruction.transform);

        // position of menu based on amount of activities
        obj.transform.localPosition = activityPos[(ams.GetInstructionAmount() - 1) % visibleActs];
    }

    /* ------------------------------------ */
    /* Keyboard Functions */
    /* ------------------------------------ */

    /** Instantiates a keyboard, its function determined by kCase */
    public void CreateKeyboard(int kCase, string text)
    {
        keyboardCase = kCase;
        isKeyboard = true;

        if (text == null)
        {
            keyboardText = "";
        }
        else
        {
            keyboardText = text;
        }

        keyboard.PresentKeyboard(keyboardText, Keyboard.LayoutType.Alpha);
    }

}
