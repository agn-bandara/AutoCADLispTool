
(defun get-bounding-box (ename / obj bbox)
    (if (setq obj (vlax-ename->vla-object ename))
        (progn
            (vla-getboundingbox obj 'minpt 'maxpt)
            (list (vlax-safearray->list minpt) (vlax-safearray->list maxpt))
        )
    )
)

(defun modify-object-properties (ename mode / obj)
    "Modify object properties based on mode: 'L for ByLayer, 'B for ByBlock"
    (if (setq obj (vlax-ename->vla-object ename))
        (progn
            ;; Pre-clean if this is a 3D solid
            (if (_is-3dsolid-p ename)
              (ClearOverridesOfSolidIfInheriting ename)
            )
            (vla-put-layer obj "0")
            (cond
                ((equal (strcase (vl-princ-to-string mode)) "L")
                    (vla-put-color obj acByLayer)
                    (vl-catch-all-apply 'vla-put-material (list obj "ByLayer")))
                ((equal (strcase (vl-princ-to-string mode)) "B")
                    (vla-put-color obj acByBlock)
                    (vl-catch-all-apply 'vla-put-material (list obj "ByBlock")))
                (T
                    (vla-put-color obj acByBlock)
                    (vl-catch-all-apply 'vla-put-material (list obj "ByBlock"))) ; Default to ByBlock
            )
            T
        )
        nil
    )
)

;; Function 1: Get Bounding box, Calculate width, Height and Depth
(defun bbox3D (ename / obj bbox minpt maxpt width height depth)
    "Returns a list with bounding box dimensions (width height depth)"
    (if (setq obj (vlax-ename->vla-object ename))
        (progn
            (vla-getboundingbox obj 'minpt 'maxpt)
            (setq minpt (vlax-safearray->list minpt)
                  maxpt (vlax-safearray->list maxpt)
                  width (- (car maxpt) (car minpt))
                  height (- (cadr maxpt) (cadr minpt))
                  depth (- (caddr maxpt) (caddr minpt)))
            (list width height depth)
        )
    
    )
  nil
)

;; Function: Interactive bounding box calculation with user selection
(defun c:bbox3DSel (/ ent obj bbox minpt maxpt width height depth dimensions result)
    "Allow user to select an object and calculate its bounding box dimensions"
    (princ "\nSelect an object to calculate bounding box dimensions: ")
    (if (setq ent (car (entsel)))
        (progn
            (if (setq obj (vlax-ename->vla-object ent))
                (progn
                    (vla-getboundingbox obj 'minpt 'maxpt)
                    (setq minpt (vlax-safearray->list minpt)
                          maxpt (vlax-safearray->list maxpt)
                          width (- (car maxpt) (car minpt))
                          height (- (cadr maxpt) (cadr minpt))
                          depth (- (caddr maxpt) (caddr minpt))
                          dimensions (list width height depth))
                    
                    (princ "\n--- Bounding Box Dimensions ---")
                    (princ (strcat "\nWidth:  " (rtos width 2 3)))
                    (princ (strcat "\nHeight: " (rtos height 2 3)))
                    (princ (strcat "\nDepth:  " (rtos depth 2 3)))
                    (princ "\n--- Coordinate Points ---")
                    (princ (strcat "\nMin Point: " (vl-princ-to-string minpt)))
                    (princ (strcat "\nMax Point: " (vl-princ-to-string maxpt)))
                    (princ (strcat "\n--- Dimensions List ---"))
                    (princ (strcat "\n(W H D): " (vl-princ-to-string dimensions)))
                    
                    ;; Return the dimensions list
                    dimensions
                )
                (progn
                    (princ "\nError: Could not process the selected object.")
                    nil
                )
            )
        )
        (progn
            (princ "\nNo object selected.")
            nil
        )
    )
    (princ)
    nil
)

;; Function 2: Update objects like bbox but skip objects with specified dimensions
(defun c:upd3DSkip (skip-dimensions mode / ss obj bbox minpt maxpt width height depth count modified i should-skip result)
    "Update objects like bbox but skip objects matching dimensions (width height depth)"
    "Use -1 or nil for dimensions to ignore. Example: (100 -1 50) skips objects with width=100 AND depth=50"
    "mode: 'L for ByLayer, 'B for ByBlock. Example: (c:upd3DSkip '(100 -1 50) 'L)"
    (if (not skip-dimensions)
        (progn
            (princ "\nError: Skip dimensions must be specified as (width height depth)")
            (princ "\nExample: (c:upd3DSkip '(100 -1 50) 'L)")
            (princ "\nMode: 'L for ByLayer, 'B for ByBlock")
            (princ)
            (exit)
        )
    )
    (if (not mode)
        (progn
            (princ "\nError: Mode must be specified ('L for ByLayer, 'B for ByBlock)")
            (princ "\nExample: (c:upd3DSkip '(100 -1 50) 'L)")
            (princ)
            (exit)
        )
    )
    (princ (strcat "\nProcessing all objects, skipping dimensions: " (vl-princ-to-string skip-dimensions)))
    (princ (strcat "\nColor mode: " (if (equal (strcase (vl-princ-to-string mode)) "L") "ByLayer" "ByBlock")))
    (if (setq ss (ssget "X"))
        (progn
            (setq count (sslength ss)
                  modified 0
                  i 0)
            (princ (strcat "\nChecking " (itoa count) " objects..."))
            (while (< i count)
                (setq obj (ssname ss i))
                (setq bbox (get-bounding-box obj))
                (if bbox
                    (progn
                        (setq minpt (car bbox)
                              maxpt (cadr bbox)
                              width (- (car maxpt) (car minpt))
                              height (- (cadr maxpt) (cadr minpt))
                              depth (- (caddr maxpt) (caddr minpt)))
                        (princ (strcat "\nObject " (itoa (1+ i)) 
                                     " - W:" (rtos width 2 3)
                                     " H:" (rtos height 2 3)
                                     " D:" (rtos depth 2 3)))
                        
                        ;; Check if object should be skipped using AND logic
                        (setq should-skip 
                            (and 
                                (or (< (nth 0 skip-dimensions) 0) (equal width (nth 0 skip-dimensions) 0.001))
                                (or (< (nth 1 skip-dimensions) 0) (equal height (nth 1 skip-dimensions) 0.001))
                                (or (< (nth 2 skip-dimensions) 0) (equal depth (nth 2 skip-dimensions) 0.001))
                            )
                        )
                        
                        (if (not should-skip)
                            (progn
                                (modify-object-properties obj mode)
                                (setq modified (1+ modified))
                                (princ (strcat " - Modified (Layer: 0, Color: " 
                                             (if (equal (strcase (vl-princ-to-string mode)) "L") "ByLayer" "ByBlock") ")"))
                            )
                            (princ " - Skipped (Matches skip criteria)")
                        )
                    )
                    (princ (strcat "\nObject " (itoa (1+ i)) " - Error: Could not get bounding box."))
                )
                (setq i (1+ i))
            )
            (progn
            (princ (strcat "\n\nSummary: " (itoa modified) " of " (itoa count) " objects modified."))
            (setq result (strcat "MODIFIED " (itoa modified)))
            )
        )
        (princ "\nNo objects found in the drawing.")
    )
    (princ)
    result
)

;; Function 3: Update objects like bbox but only consider objects with specified dimensions
(defun c:upd3DSel (select-dimensions mode / ss obj bbox minpt maxpt width height depth count modified i should-select)
    "Update objects like bbox but only consider objects matching dimensions (width height depth)"
    "Use -1 or nil for dimensions to ignore. Example: (100 -1 50) selects objects with width=100 AND depth=50"
    "mode: 'L for ByLayer, 'B for ByBlock. Example: (c:upd3DSel '(100 -1 50) 'L)"
    (if (not select-dimensions)
        (progn
            (princ "\nError: Select dimensions must be specified as (width height depth)")
            (princ "\nExample: (c:upd3DSel '(100 -1 50) 'L)")
            (princ "\nMode: 'L for ByLayer, 'B for ByBlock")
            (princ)
            (exit)
        )
    )
    (if (not mode)
        (progn
            (princ "\nError: Mode must be specified ('L for ByLayer, 'B for ByBlock)")
            (princ "\nExample: (c:upd3DSel '(100 -1 50) 'L)")
            (princ)
            (exit)
        )
    )
    (princ (strcat "\nProcessing objects with dimensions: " (vl-princ-to-string select-dimensions)))
    (princ (strcat "\nColor mode: " (if (equal (strcase (vl-princ-to-string mode)) "L") "ByLayer" "ByBlock")))
    (if (setq ss (ssget "X"))
        (progn
            (setq count (sslength ss)
                  modified 0
                  i 0)
            (princ (strcat "\nChecking " (itoa count) " objects..."))
            (while (< i count)
                (setq obj (ssname ss i))
                (setq bbox (get-bounding-box obj))
                (if bbox
                    (progn
                        (setq minpt (car bbox)
                              maxpt (cadr bbox)
                              width (- (car maxpt) (car minpt))
                              height (- (cadr maxpt) (cadr minpt))
                              depth (- (caddr maxpt) (caddr minpt)))
                        (princ (strcat "\nObject " (itoa (1+ i)) 
                                     " - W:" (rtos width 2 3)
                                     " H:" (rtos height 2 3)
                                     " D:" (rtos depth 2 3)))
                        
                        ;; Check if object should be selected using AND logic
                        (setq should-select 
                            (and 
                                (or (< (nth 0 select-dimensions) 0) (equal width (nth 0 select-dimensions) 0.001))
                                (or (< (nth 1 select-dimensions) 0) (equal height (nth 1 select-dimensions) 0.001))
                                (or (< (nth 2 select-dimensions) 0) (equal depth (nth 2 select-dimensions) 0.001))
                            )
                        )
                        
                        (if should-select
                            (progn
                                (modify-object-properties obj mode)
                                (setq modified (1+ modified))
                                (princ (strcat " - Modified (Layer: 0, Color: " 
                                             (if (equal (strcase (vl-princ-to-string mode)) "L") "ByLayer" "ByBlock") ")"))
                            )
                            (princ " - Skipped (Does not match selection criteria)")
                        )
                    )
                    (princ (strcat "\nObject " (itoa (1+ i)) " - Error: Could not get bounding box."))
                )
                (setq i (1+ i))
            )
            (progn
              (setq result (strcat "MODIFIED " (itoa modified)))
              (princ (strcat "\n\nSummary: " (itoa modified) " of " (itoa count) " objects modified."))
            )
        )
        (princ "\nNo objects found in the drawing.")
    )
    (princ)
    result
)

;; Function 4: Check and count objects with specified bounding box dimensions
(defun c:bbox3DCheck (check-dimensions / ss obj bbox minpt maxpt width height depth count matched i should-match match-list result)
    "Count objects with matching bounding box dimensions (width height depth)"
    "Use -1 for dimensions to ignore. Example: (100 -1 50) counts objects with width=100 AND depth=50"
    (if (not check-dimensions)
        (progn
            (princ "\nError: Check dimensions must be specified as (width height depth)")
            (princ "\nExample: (c:bbox3DCheck '(100 -1 50))")
            (princ "\nExample: (c:bbox3DCheck '(100 200 50)) - checks all three dimensions")
            (princ "\nExample: (c:bbox3DCheck '(-1 200 -1)) - checks only height dimension")
            (princ)
            (exit)
        )
    )
    (princ (strcat "\nChecking objects with dimensions: " (vl-princ-to-string check-dimensions)))
    (princ "\nDimension criteria:")
    (princ (strcat "\n  Width:  " (if (< (nth 0 check-dimensions) 0) "Any" (rtos (nth 0 check-dimensions) 2 3))))
    (princ (strcat "\n  Height: " (if (< (nth 1 check-dimensions) 0) "Any" (rtos (nth 1 check-dimensions) 2 3))))
    (princ (strcat "\n  Depth:  " (if (< (nth 2 check-dimensions) 0) "Any" (rtos (nth 2 check-dimensions) 2 3))))
    
    (if (setq ss (ssget "X"))
        (progn
            (setq count (sslength ss)
                  matched 0
                  i 0
                  match-list '())
            (princ (strcat "\n\nAnalyzing " (itoa count) " objects..."))
            (while (< i count)
                (setq obj (ssname ss i))
                (setq bbox (get-bounding-box obj))
                (if bbox
                    (progn
                        (setq minpt (car bbox)
                              maxpt (cadr bbox)
                              width (- (car maxpt) (car minpt))
                              height (- (cadr maxpt) (cadr minpt))
                              depth (- (caddr maxpt) (caddr minpt)))
                        
                        ;; Check if object matches the criteria using AND logic
                        (setq should-match 
                            (and 
                                (or (< (nth 0 check-dimensions) 0) (equal width (nth 0 check-dimensions) 0.001))
                                (or (< (nth 1 check-dimensions) 0) (equal height (nth 1 check-dimensions) 0.001))
                                (or (< (nth 2 check-dimensions) 0) (equal depth (nth 2 check-dimensions) 0.001))
                            )
                        )
                        
                        (if should-match
                            (progn
                                (setq matched (1+ matched))
                                (setq match-list (cons (list obj width height depth) match-list))
                                (princ (strcat "\nMatch " (itoa matched) 
                                             " - W:" (rtos width 2 3)
                                             " H:" (rtos height 2 3)
                                             " D:" (rtos depth 2 3)))
                            )
                        )
                    )
                )
                (setq i (1+ i))
            )
            (princ "\n")
            (princ "\n--- RESULTS ---")
            (princ (strcat "\nTotal objects checked: " (itoa count)))
            (princ (strcat "\nObjects matching criteria: " (itoa matched)))
            (if (> matched 0)
                (progn
                    (princ "\n\nMatched objects summary:")
                    (setq i 1)
                    (foreach match-item (reverse match-list)
                        (princ (strcat "\n  " (itoa i) ". W:" 
                                     (rtos (nth 1 match-item) 2 3) 
                                     " H:" (rtos (nth 2 match-item) 2 3)
                                     " D:" (rtos (nth 3 match-item) 2 3)))
                        (setq i (1+ i))
                    )
                    (setq result (strcat "FOUND " (itoa matched)))
                )
                (progn
                  (setq result "NOT_FOUND")
                  (princ "\nNo objects found matching the specified criteria.")
                )
            )
        )
        (progn
            (setq result "NO_OBJECTS")
            (princ "\nNo objects found in the drawing.")
            0
        )
    )
    (princ)
    ;; Return the count of matched objects
    result
)

;; Helper function: bbox3DCheck without the c: prefix for programmatic use
(defun bbox3DCheck (check-dimensions / ss obj bbox minpt maxpt width height depth count matched i should-match)
    "Count objects with matching bounding box dimensions - returns count only"
    "Use -1 for dimensions to ignore. Example: (bbox3DCheck '(100 -1 50))"
    (if (setq ss (ssget "X"))
        (progn
            (setq count (sslength ss)
                  matched 0
                  i 0)
            (while (< i count)
                (setq obj (ssname ss i))
                (setq bbox (get-bounding-box obj))
                (if bbox
                    (progn
                        (setq minpt (car bbox)
                              maxpt (cadr bbox)
                              width (- (car maxpt) (car minpt))
                              height (- (cadr maxpt) (cadr minpt))
                              depth (- (caddr maxpt) (caddr minpt)))
                        
                        ;; Check if object matches the criteria using AND logic
                        (setq should-match 
                            (and 
                                (or (< (nth 0 check-dimensions) 0) (equal width (nth 0 check-dimensions) 0.001))
                                (or (< (nth 1 check-dimensions) 0) (equal height (nth 1 check-dimensions) 0.001))
                                (or (< (nth 2 check-dimensions) 0) (equal depth (nth 2 check-dimensions) 0.001))
                            )
                        )
                        
                        (if should-match
                            (setq matched (1+ matched))
                        )
                    )
                )
                (setq i (1+ i))
            )
            matched
        )
        0
    )
)

;; Function 5: Create 3D surfaces in the middle of bounding boxes
(defun c:bbox3DSurface (filter-dimensions axis / ss obj bbox minpt maxpt width height depth count created i should-match center-pt pt1 pt2 pt3 pt4)
    "Create rectangular 3D surfaces in the middle of objects matching dimensions"
    "filter-dimensions: (width height depth) - Use -1 to ignore dimension"
    "axis: 'X', 'Y', or 'Z' - perpendicular axis for the surface"
    "Example: (c:bbox3DSurface '(100 -1 50) 'Z) - creates XY plane surfaces for objects with W=100, D=50"
    
    (if (not filter-dimensions)
        (progn
            (princ "\nError: Filter dimensions must be specified as (width height depth)")
            (princ "\nExample: (c:bbox3DSurface '(100 -1 50) 'Z)")
            (princ)
            (exit)
        )
    )
    
    (if (not axis)
        (progn
            (princ "\nError: Perpendicular axis must be specified ('X', 'Y', or 'Z')")
            (princ "\nExample: (c:bbox3DSurface '(100 -1 50) 'Z)")
            (princ)
            (exit)
        )
    )
    
    ;; Validate axis input
    (if (not (member (strcase (vl-princ-to-string axis)) '("X" "Y" "Z")))
        (progn
            (princ "\nError: Axis must be 'X', 'Y', or 'Z'")
            (princ)
            (exit)
        )
    )
    
    (setq axis (strcase (vl-princ-to-string axis)))
    
    (princ (strcat "\nCreating 3D surfaces for objects with dimensions: " (vl-princ-to-string filter-dimensions)))
    (princ (strcat "\nSurface perpendicular to " axis "-axis"))
    (princ "\nDimension criteria:")
    (princ (strcat "\n  Width:  " (if (< (nth 0 filter-dimensions) 0) "Any" (rtos (nth 0 filter-dimensions) 2 3))))
    (princ (strcat "\n  Height: " (if (< (nth 1 filter-dimensions) 0) "Any" (rtos (nth 1 filter-dimensions) 2 3))))
    (princ (strcat "\n  Depth:  " (if (< (nth 2 filter-dimensions) 0) "Any" (rtos (nth 2 filter-dimensions) 2 3))))
    
    (if (setq ss (ssget "X"))
        (progn
            (setq count (sslength ss)
                  created 0
                  i 0)
            (princ (strcat "\n\nProcessing " (itoa count) " objects..."))
            (while (< i count)
                (setq obj (ssname ss i))
                (setq bbox (get-bounding-box obj))
                (if bbox
                    (progn
                        (setq minpt (car bbox)
                              maxpt (cadr bbox)
                              width (- (car maxpt) (car minpt))
                              height (- (cadr maxpt) (cadr minpt))
                              depth (- (caddr maxpt) (caddr minpt)))
                        
                        ;; Check if object matches the criteria using AND logic
                        (setq should-match 
                            (and 
                                (or (< (nth 0 filter-dimensions) 0) (equal width (nth 0 filter-dimensions) 0.001))
                                (or (< (nth 1 filter-dimensions) 0) (equal height (nth 1 filter-dimensions) 0.001))
                                (or (< (nth 2 filter-dimensions) 0) (equal depth (nth 2 filter-dimensions) 0.001))
                            )
                        )
                        
                        (if should-match
                            (progn
                                ;; Calculate center point of bounding box
                                (setq center-pt (list 
                                    (/ (+ (car minpt) (car maxpt)) 2.0)
                                    (/ (+ (cadr minpt) (cadr maxpt)) 2.0)
                                    (/ (+ (caddr minpt) (caddr maxpt)) 2.0)
                                ))
                                
                                ;; Create surface based on perpendicular axis
                                (cond
                                    ;; Surface perpendicular to X-axis (YZ plane)
                                    ((equal axis "X")
                                        (setq pt1 (list (car center-pt) (cadr minpt) (caddr minpt))
                                              pt2 (list (car center-pt) (cadr maxpt) (caddr minpt))
                                              pt3 (list (car center-pt) (cadr maxpt) (caddr maxpt))
                                              pt4 (list (car center-pt) (cadr minpt) (caddr maxpt)))
                                    )
                                    ;; Surface perpendicular to Y-axis (XZ plane)
                                    ((equal axis "Y")
                                        (setq pt1 (list (car minpt) (cadr center-pt) (caddr minpt))
                                              pt2 (list (car maxpt) (cadr center-pt) (caddr minpt))
                                              pt3 (list (car maxpt) (cadr center-pt) (caddr maxpt))
                                              pt4 (list (car minpt) (cadr center-pt) (caddr maxpt)))
                                    )
                                    ;; Surface perpendicular to Z-axis (XY plane)
                                    ((equal axis "Z")
                                        (setq pt1 (list (car minpt) (cadr minpt) (caddr center-pt))
                                              pt2 (list (car maxpt) (cadr minpt) (caddr center-pt))
                                              pt3 (list (car maxpt) (cadr maxpt) (caddr center-pt))
                                              pt4 (list (car minpt) (cadr maxpt) (caddr center-pt)))
                                    )
                                )
                                
                                ;; Create 3D face using entmake
                                (entmake (list 
                                    '(0 . "3DFACE")
                                    '(8 . "BBOX_SURFACES")
                                    '(62 . 1) ; Red color
                                    (cons 10 pt1)
                                    (cons 11 pt2)
                                    (cons 12 pt3)
                                    (cons 13 pt4)
                                ))
                                
                                (setq created (1+ created))
                                (princ (strcat "\nSurface " (itoa created) 
                                             " created - W:" (rtos width 2 3)
                                             " H:" (rtos height 2 3)
                                             " D:" (rtos depth 2 3)
                                             " Center:" (vl-princ-to-string center-pt)))
                            )
                        )
                    )
                    (princ (strcat "\nObject " (itoa (1+ i)) " - Error: Could not get bounding box."))
                )
                (setq i (1+ i))
            )
            (princ "\n")
            (princ "\n--- RESULTS ---")
            (princ (strcat "\nTotal objects processed: " (itoa count)))
            (princ (strcat "\n3D surfaces created: " (itoa created)))
            (if (> created 0)
                (princ (strcat "\nSurfaces created on layer: BBOX_SURFACES (Red color)"))
                (princ "\nNo surfaces created - no objects matched the criteria.")
            )
            created
        )
        (progn
            (princ "\nNo objects found in the drawing.")
            0
        )
    )
    (princ)
)

;;; ---------- helpers ---------------------------------------------------------------------------------------------------------------
(defun _is-manual-colored-el (el / c)
  (cond
    ((or (assoc 420 el) (assoc 430 el)) T)     ; TrueColor / ColorBook
    ((setq c (cdr (assoc 62 el)))              ; ACI: 1..255 = manual; 256=ByLayer; 0=ByBlock
     (and (<= 1 (abs c)) (<= (abs c) 255)))
    (T nil)
  )
)

(defun _strip-noncolor-overrides-ename (en / el)
  ;; Remove overrides that can mask color inheritance; DO NOT touch 62/420/430
  (setq el (entget en '("*")))
  (setq el (vl-remove-if
             '(lambda (p) (member (car p) '(390 347 440 450 451 452 453 454 455 463)))
             el))
  (entmod el)
)

(defun _is-3dsolid-p (en)
  (= (cdr (assoc 0 (entget en))) "3DSOLID")
)

;;; Clear overrides for a 3D solid, but only if it's inheriting color
(defun ClearOverridesOfSolidIfInheriting (en / el inheriting)
  (if (not (_is-3dsolid-p en))
    (progn (princ "\nNot a 3D solid.") nil)
    (progn
      (setq el (entget en '("*")))
      (setq inheriting (not (_is-manual-colored-el el))) ; True if ByLayer/ByBlock/no TrueColor

      ;; Always strip non-color overrides; safe either way.
      (_strip-noncolor-overrides-ename en)
    )
  )
)

;;; ---------- Layer Purge & Flattening Extensions ---------------------------------------------------------------------

;; Helper: Purge all layers except "0"
(defun _purge-unused-layers ( / )
  (setvar "CLAYER" "0")
  (princ "\nPurging unused layers...")
  (repeat 4
    (vl-cmdf "-PURGE" "LA" "*" "N")
  )
  (princ "\nUnused layers purged.")
)

;; Helper: Move all sub-entities within block definitions to layer "0"
(defun _move-block-entities-to-layer-0 (mode / doc blocks blk obj)
  (setq doc (vla-get-activedocument (vlax-get-acad-object)))
  (setq blocks (vla-get-blocks doc))
  (vlax-for blk blocks
    ;; Skip Xrefs and Layout blocks
    (if (and (= (vla-get-isxref blk) :vlax-false)
             (= (vla-get-islayout blk) :vlax-false))
      (vlax-for obj blk
        (vl-catch-all-apply 'vla-put-layer (list obj "0"))
        (cond
          ((equal (strcase (vl-princ-to-string mode)) "L")
           (vl-catch-all-apply 'vla-put-color (list obj acByLayer))
           (vl-catch-all-apply 'vla-put-material (list obj "ByLayer")))
          ((equal (strcase (vl-princ-to-string mode)) "B")
           (vl-catch-all-apply 'vla-put-color (list obj acByBlock))
           (vl-catch-all-apply 'vla-put-material (list obj "ByBlock")))
        )
      )
    )
  )
)

;; Function: Move ALL objects (drawing & blocks) to Layer 0 and Purge all other layers
(defun c:upd3DAllAndPurge (mode / ss i count obj modified result)
  "Move ALL objects in drawing and block definitions to Layer 0, set color mode ('L for ByLayer, 'B for ByBlock), and purge all other layers."
  "Example usage: (c:upd3DAllAndPurge 'L) or (c:upd3DAllAndPurge 'B)"
  (if (not mode)
      (progn
          (princ "\nError: Mode must be specified ('L for ByLayer, 'B for ByBlock)")
          (princ "\nExample: (c:upd3DAllAndPurge 'L)")
          (princ)
          (exit)
      )
  )
  (princ "\n--- Setting Layer 0 as current layer ---")
  (setvar "CLAYER" "0")
  (princ "\n--- Moving all drawing objects to Layer 0 ---")
  (if (setq ss (ssget "X"))
      (progn
          (setq count (sslength ss)
                modified 0
                i 0)
          (while (< i count)
              (setq obj (ssname ss i))
              (if (modify-object-properties obj mode)
                  (setq modified (1+ modified))
              )
              (setq i (1+ i))
          )
          (princ (strcat "\nModified " (itoa modified) " drawing objects."))
      )
  )
  (princ "\n--- Moving nested block entities to Layer 0 ---")
  (_move-block-entities-to-layer-0 mode)
  
  (princ "\n--- Purging unused layers ---")
  (_purge-unused-layers)
  
  (setq result (strcat "COMPLETED: " (itoa modified) " objects moved to Layer 0, layers purged."))
  (princ (strcat "\n" result))
  (princ)
  result
)

;; Function: Standalone purge all unused layers except Layer 0
(defun c:upd3DPurgeLayers ()
  "Purge all unused layers except Layer 0"
  (_purge-unused-layers)
  (princ)
)

;; Function: Update objects but skip objects below specified max dimensions (width height depth)
(defun c:upd3DSkipBelow (max-dimensions mode / ss obj bbox minpt maxpt width height depth count modified i should-skip result)
    "Update objects like bbox but skip objects where width < max-width, height < max-height, depth < max-depth."
    "Use -1 to ignore a dimension threshold. Example: (c:upd3DSkipBelow '(40 170 -1) 'L)"
    (if (not max-dimensions)
        (progn
            (princ "\nError: Max dimensions must be specified as (max-width max-height max-depth)")
            (princ "\nExample: (c:upd3DSkipBelow '(40 170 -1) 'L)")
            (princ "\nMode: 'L for ByLayer, 'B for ByBlock")
            (princ)
            (exit)
        )
    )
    (if (not mode)
        (progn
            (princ "\nError: Mode must be specified ('L for ByLayer, 'B for ByBlock)")
            (princ "\nExample: (c:upd3DSkipBelow '(40 170 -1) 'L)")
            (princ)
            (exit)
        )
    )
    (princ (strcat "\nProcessing objects, skipping if below dimensions: " (vl-princ-to-string max-dimensions)))
    (princ (strcat "\nColor/Material mode: " (if (equal (strcase (vl-princ-to-string mode)) "L") "ByLayer" "ByBlock")))
    (if (setq ss (ssget "X"))
        (progn
            (setq count (sslength ss)
                  modified 0
                  i 0)
            (princ (strcat "\nChecking " (itoa count) " objects..."))
            (while (< i count)
                (setq obj (ssname ss i))
                (setq bbox (get-bounding-box obj))
                (if bbox
                    (progn
                        (setq minpt (car bbox)
                              maxpt (cadr bbox)
                              width (- (car maxpt) (car minpt))
                              height (- (cadr maxpt) (cadr minpt))
                              depth (- (caddr maxpt) (caddr minpt)))
                        (princ (strcat "\nObject " (itoa (1+ i)) 
                                     " - W:" (rtos width 2 3)
                                     " H:" (rtos height 2 3)
                                     " D:" (rtos depth 2 3)))
                        
                        ;; Check if object should be skipped (specified dimensions are strictly less than max-dimensions)
                        (setq should-skip 
                            (and 
                                (or (< (nth 0 max-dimensions) 0) (< width (nth 0 max-dimensions)))
                                (or (< (nth 1 max-dimensions) 0) (< height (nth 1 max-dimensions)))
                                (or (< (nth 2 max-dimensions) 0) (< depth (nth 2 max-dimensions)))
                            )
                        )
                        
                        (if (not should-skip)
                            (progn
                                (modify-object-properties obj mode)
                                (setq modified (1+ modified))
                                (princ (strcat " - Modified (Layer: 0, Color/Material: " 
                                             (if (equal (strcase (vl-princ-to-string mode)) "L") "ByLayer" "ByBlock") ")"))
                            )
                            (princ " - Skipped (Below dimension thresholds)")
                        )
                    )
                    (princ (strcat "\nObject " (itoa (1+ i)) " - Error: Could not get bounding box."))
                )
                (setq i (1+ i))
            )
            (progn
            (princ (strcat "\n\nSummary: " (itoa modified) " of " (itoa count) " objects modified."))
            (setq result (strcat "MODIFIED " (itoa modified)))
            )
        )
        (princ "\nNo objects found in the drawing.")
    )
    (princ)
    result
)

;; Function: Update objects but skip objects over specified min dimensions (width height depth)
(defun c:upd3DSkipOver (min-dimensions mode / ss obj bbox minpt maxpt width height depth count modified i should-skip result)
    "Update objects like bbox but skip objects where width > min-width, height > min-height, depth > min-depth."
    "Use -1 to ignore a dimension threshold. Example: (c:upd3DSkipOver '(40 170 -1) 'L)"
    (if (not min-dimensions)
        (progn
            (princ "\nError: Min dimensions must be specified as (min-width min-height min-depth)")
            (princ "\nExample: (c:upd3DSkipOver '(40 170 -1) 'L)")
            (princ "\nMode: 'L for ByLayer, 'B for ByBlock")
            (princ)
            (exit)
        )
    )
    (if (not mode)
        (progn
            (princ "\nError: Mode must be specified ('L for ByLayer, 'B for ByBlock)")
            (princ "\nExample: (c:upd3DSkipOver '(40 170 -1) 'L)")
            (princ)
            (exit)
        )
    )
    (princ (strcat "\nProcessing objects, skipping if over dimensions: " (vl-princ-to-string min-dimensions)))
    (princ (strcat "\nColor/Material mode: " (if (equal (strcase (vl-princ-to-string mode)) "L") "ByLayer" "ByBlock")))
    (if (setq ss (ssget "X"))
        (progn
            (setq count (sslength ss)
                  modified 0
                  i 0)
            (princ (strcat "\nChecking " (itoa count) " objects..."))
            (while (< i count)
                (setq obj (ssname ss i))
                (setq bbox (get-bounding-box obj))
                (if bbox
                    (progn
                        (setq minpt (car bbox)
                              maxpt (cadr bbox)
                              width (- (car maxpt) (car minpt))
                              height (- (cadr maxpt) (cadr minpt))
                              depth (- (caddr maxpt) (caddr minpt)))
                        (princ (strcat "\nObject " (itoa (1+ i)) 
                                     " - W:" (rtos width 2 3)
                                     " H:" (rtos height 2 3)
                                     " D:" (rtos depth 2 3)))
                        
                        ;; Check if object should be skipped (specified dimensions are strictly greater than min-dimensions)
                        (setq should-skip 
                            (and 
                                (or (< (nth 0 min-dimensions) 0) (> width (nth 0 min-dimensions)))
                                (or (< (nth 1 min-dimensions) 0) (> height (nth 1 min-dimensions)))
                                (or (< (nth 2 min-dimensions) 0) (> depth (nth 2 min-dimensions)))
                            )
                        )
                        
                        (if (not should-skip)
                            (progn
                                (modify-object-properties obj mode)
                                (setq modified (1+ modified))
                                (princ (strcat " - Modified (Layer: 0, Color/Material: " 
                                             (if (equal (strcase (vl-princ-to-string mode)) "L") "ByLayer" "ByBlock") ")"))
                            )
                            (princ " - Skipped (Over dimension thresholds)")
                        )
                    )
                    (princ (strcat "\nObject " (itoa (1+ i)) " - Error: Could not get bounding box."))
                )
                (setq i (1+ i))
            )
            (progn
            (princ (strcat "\n\nSummary: " (itoa modified) " of " (itoa count) " objects modified."))
            (setq result (strcat "MODIFIED " (itoa modified)))
            )
        )
        (princ "\nNo objects found in the drawing.")
    )
    (princ)
    result
)

