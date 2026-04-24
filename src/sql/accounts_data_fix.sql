UPDATE accounts AS a
SET user_id = v.user_id
FROM (VALUES
    -- replace 'data_pairs' with required data
    :data_pairs) AS v(user_id, target_account_id)
WHERE a.account_id = v.target_account_id;