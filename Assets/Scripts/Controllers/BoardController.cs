using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;
    private int a;
    public static int b;
    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {

        m_gameManager = gameManager;
       
        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        a = m_board.GetAllCells();
        b = 0;
        Fill();
    }

    private void Fill()
    {
        m_board.Fill();
        FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                StopHints();
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint)
            {
                m_timeAfterFill = 0f;
                //ShowHint();
            }
        }

        //if (Input.GetMouseButtonDown(0))
        //{
        //    var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        //    if (hit.collider != null)
        //    {
        //        m_isDragging = true;
        //        m_hitCollider = hit.collider;
        //    }
        //}

        //if (Input.GetMouseButtonUp(0))
        //{
        //    ResetRayCast();
        //}

        //if (Input.GetMouseButton(0) && m_isDragging)
        //{
        //    var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        //    if (hit.collider != null)
        //    {
        //        if (m_hitCollider != null && m_hitCollider != hit.collider)
        //        {
        //            StopHints();

        //            Cell c1 = m_hitCollider.GetComponent<Cell>();
        //            Cell c2 = hit.collider.GetComponent<Cell>();
        //            if (AreItemsNeighbor(c1, c2))
        //            {
        //                IsBusy = true;
        //                SetSortingLayer(c1, c2);
        //                m_board.Swap(c1, c2, () =>
        //                {
        //                    FindMatchesAndCollapse(c1, c2);
        //                });

        //                ResetRayCast();
        //            }
        //        }
        //    }
        //    else
        //    {
        //        ResetRayCast();
        //    }
        //}
        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Cell selectedCell = hit.collider.GetComponent<Cell>();
                if (selectedCell != null && !selectedCell.IsEmpty)
                {
                    HandleCellSelection(selectedCell);
                    
                }
            }
        }
    }
    private void HandleCellSelection(Cell selectedCell)
    {
        if (selectedCell.Item == null) return; // Nếu ô không có item, không làm gì cả.
        if (selectedCell.isbottomcell) return;
        string prefabName = selectedCell.Item.GetItemPrefabName(); // Lấy tên prefab
        selectedCell.ExplodeItem(); // Xóa item khỏi ô hiện tại
        b++;
        Debug.Log(b);
        Board.allcell--;
        // Tìm một ô trống trong bottomCells
        Cell bottomCell = m_board.GetAvailableBottomCell();
        if (bottomCell != null && !string.IsNullOrEmpty(prefabName))
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName); // Load prefab từ Resources
            if (prefab)
            {
                GameObject newItemObject = Instantiate(prefab, bottomCell.transform.position, Quaternion.identity);

                Item newItem = new Item();
                newItem.SetCell(bottomCell);
                newItem.View = newItemObject.transform;

                bottomCell.Assign(newItem);
            }
        }

        m_board.CheckAndClearMatchingItems();
        
        // Kiểm tra trạng thái game
        CheckGameState();

    }
    private void CheckGameState()
    {
        bool isGameOver = false;
        bool isGameWin = false;
        if (Board.allcell == 0 && isGameOver ==false)
        {
            isGameWin = true;
        }
        // Kiểm tra nếu tất cả bottomCells đã đầy => Thua
        
        if (b >= 9)
        {
            isGameOver = true;
        }
        if (isGameWin)
        {
            Debug.Log("Game Win");
            m_gameManager.SetState(GameManager.eStateGame.Win);
        }
        if (isGameOver)
        {
            Debug.Log("💀 GAME OVER!");
            m_gameManager.SetState(GameManager.eStateGame.GAME_OVER);
        }
    }




    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        List<Cell> matches = m_board.FindFirstMatch();
        if (matches.Count > 0)
        {
            CollapseMatches(matches);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;
                m_timeAfterFill = 0f;
            }
            else
            {
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }
    private void CollapseMatches(List<Cell> matches)
    {
        foreach (Cell cell in matches)
        {
            cell.ExplodeItem();
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }
    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }
}
